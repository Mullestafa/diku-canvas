#r "nuget:SixLabors.ImageSharp"
#r "nuget: SixLabors.ImageSharp.Drawing, 1.0.0-beta15"
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Drawing
open SixLabors.ImageSharp.Drawing.Processing
open SixLabors.Fonts

/////////////////////////////////
// Jon's work on the interface //
/////////////////////////////////

type HPosition = Top | Center | Bottom 
type VPosition = Left | Center | Right 
type Draw = int -> int -> int -> int -> unit // A function that draws a shape
type Picture = 
  Empty of int*int
  | Leaf of Draw*int*int
  | Horizontal of Picture*Picture*int*int
  | Vertical of Picture*Picture*int*int
  | OnTop of Picture*Picture*int*int
  | AffineTransform of Picture*System.Numerics.Matrix3x2*Color*int*int
  | Scale of Picture*float*float*int*int
  | Rotate of Picture*int*int*float*Color*int*int
  | Translate of Picture*int*int*Color*int*int
  | Crop of Picture*int*int*int*int
let getSize (p:Picture): int*int =
  match p with
    Empty(w, h)
    | Leaf(_,w,h)
    | Horizontal(_,_,w,h)
    | Vertical(_,_,w,h)
    | OnTop(_,_,w,h)
    | AffineTransform(_,_,_,w,h)
    | Scale(_,_,_,w,h)
    | Rotate(_,_,_,_,_,w,h)
    | Translate(_,_,_,_,w,h)
    | Crop(_,_,_,w,h) -> w,h
let rec horizontal (pic1:Picture) (pos:HPosition) (pic2:Picture): Picture =
  let w1,h1 = getSize pic1
  let w2,h2 = getSize pic2
  let w, h = w1+w2, (max h1 h2)
  let dh = abs (h1-h2)
  let full1 = Empty(w1,dh)
  let half1 = Empty(w1,dh/2)
  let full2 = Empty(w2,dh)
  let half2 = Empty(w2,dh/2)
  match pos, h1, h2 with
    | Top, h1, h2 when h1 > h2 -> Horizontal(pic1, vertical pic2 Left full2, w, h)
    | Top, h1, h2 when h1 = h2 -> Horizontal(pic1, pic2, w, h)
    | Top, _, _ -> Horizontal(vertical pic1 Left full1, pic2, w, h)
    | HPosition.Center, h1, h2 when h1 > h2 -> horizontal pic1 Top (vertical half2 Left pic2)
    | HPosition.Center, h1, h2 when h1 = h2 -> Horizontal(pic1, pic2, w, h)
    | HPosition.Center, _, _ -> horizontal (vertical half1 Left pic1) Top pic2
    | Bottom, h1, h2 when h1 > h2 -> Horizontal(pic1, vertical full2 Left pic2, w, h)
    | Bottom, h1, h2 when h1 = h2 -> Horizontal(pic1, pic2, w, h)
    | Bottom, _, _ -> Horizontal(vertical full1 Left pic1, pic2, w, h)
and vertical (pic1:Picture) (pos:VPosition) (pic2:Picture): Picture =
  let w1,h1 = getSize pic1
  let w2,h2 = getSize pic2
  let w, h = (max w1 w2), h1 + h2
  let dw = abs (w1-w2)
  let full1 = Empty(dw,h1)
  let half1 = Empty(dw/2,h1)
  let full2 = Empty(dw,h2)
  let half2 = Empty(dw/2,h2)
  match pos, w1, w2 with
    | Left, w1, w2 when w1 > w2 -> Vertical(pic1, horizontal pic2 Top full2, w, h)
    | Left, w1, w2 when w1 = w2 -> Vertical(pic1, pic2, w, h)
    | Left, _, _ -> Vertical(horizontal pic1 Top full1, pic2, w, h)
    | VPosition.Center, w1, w2 when w1 > w2 -> vertical pic1 Left (horizontal half2 Top pic2)
    | VPosition.Center, w1, w2 when w1 = w2 -> Vertical(pic1, pic2, w, h)
    | VPosition.Center, _, _ -> vertical (horizontal half1 Top pic1) Left pic2 
    | Right, w1, w2 when w1 > w2 -> Vertical(pic1, horizontal full2 Top pic2, w, h)
    | Right, w1, w2 when w1 = w2 -> Vertical(pic1, pic2, w, h)
    | Right, _, _ -> Vertical(horizontal full1 Top pic1, pic2, w, h)
let round (a:float): int = int (a+0.5)
let ontop (pic1:Picture) (a:float) (b:float) (pic2:Picture): Picture =
  if a < 0 || a > 1 || b < 0 || b > 1 then 
    raise (System.ArgumentOutOfRangeException ("a and b must be in [0,1]"))
  let w1,h1 = getSize pic1
  let w2,h2 = getSize pic2
  let s = float (w1-w2)
  let w, h = (max w1 w2), (max h1 h2)
  let pic3, pic4 = 
    match s, int (a*s) with
      | s, 0 when s > 0.0 -> pic1, (horizontal pic2 Top (Empty(w1-w2,h2)))
      | s, dw when s > 0.0 && dw = w1-w2 -> pic1, horizontal (Empty(dw, h2)) Top pic2
      | s, dw when s > 0.0 -> pic1, (horizontal (horizontal (Empty(dw, h2)) Top pic2) Top (Empty(w1-dw-w2,h2)))
      | s, 0 when s < 0.0 -> (horizontal pic1 Top (Empty(w2-w1,h1))), pic2
      | s, dw when s < 0.0 && dw = w1-w2 -> horizontal (Empty(-dw, h1)) Top pic1, pic2
      | s, dw when s < 0.0 -> (horizontal (horizontal (Empty(-dw, h1)) Top pic1) Top (Empty(w2+dw-w1,h1))), pic2
      | _ -> pic1, pic2
  let _,h3 = getSize pic3
  let _,h4 = getSize pic4
  let t = float (h3-h4)
  let pic5, pic6 = 
    match t, int (b*t) with
      | t, 0 when t > 0.0 -> pic3, (vertical pic4 Left (Empty(w, h3-h4)))
      | t, dh when t > 0.0 && dh = h3-h4 -> pic3, vertical (Empty(w, dh)) Left pic4
      | t, dh when t > 0.0 -> pic3, (vertical (vertical (Empty(w, dh)) Left pic4) Left (Empty(w, h3-dh-h4)))
      | t, 0 when t < 0.0 -> (vertical pic3 Left (Empty(w, h4-h3))), pic4
      | t, dh when t < 0.0 && dh = h3-h4 -> vertical (Empty(w,-dh)) Left pic3, pic4
      | t, dh when t < 0.0 -> (vertical (vertical (Empty(w,-dh)) Left pic3) Left (Empty(w, h4+dh-h3))), pic4
      | _ -> pic3, pic4
  OnTop(pic5, pic6, w, h)

let rec sharpDraw (pic:Picture) : Image<Rgba32> =
  let affinetransform (p:Picture) (M:System.Numerics.Matrix3x2) (c: Color) (w:int) (h:int): Image<Rgba32> =
    let I = sharpDraw p
    let transformation = AffineTransformBuilder().AppendMatrix(M)
    I.Mutate(fun x -> x.Transform(transformation)|>ignore)
    I.Mutate(fun x -> x.Crop(min I.Width w, min I.Height h)
                       .Resize(ResizeOptions(Position = AnchorPositionMode.TopLeft,
                                             Size = Size(w, h), 
                                             Mode = ResizeMode.BoxPad))
                       .BackgroundColor(c)|>ignore)
    I

  match pic with
    | Empty(a,b) -> 
      new Image<Rgba32>(a,b,Color.LightGray)
    | Leaf(f, a, b) ->
      new Image<Rgba32>(a,b,Color.Red) //f c x y; (a, b)
    | Horizontal(p1, p2, a, b) ->
      let w1,_ = getSize p1
      let I = new Image<Rgba32>(a,b)
      let left = sharpDraw p1
      let right = sharpDraw p2
      I.Mutate(fun x -> 
        x.DrawImage(left, Point(0, 0), 1f) |> ignore
        x.DrawImage(right, Point(w1, 0), 1f) |> ignore)
      I
    | Vertical(p1, p2, a, b) ->
      let _,h1 = getSize p1
      let I = new Image<Rgba32>(a,b)
      let top = sharpDraw p1
      let bottom = sharpDraw p2
      I.Mutate(fun x -> 
        x.DrawImage(top, Point(0, 0), 1f) |> ignore
        x.DrawImage(bottom, Point(0, h1), 1f) |> ignore)
      I
    | OnTop(p1, p2, a, b) ->
      let I = new Image<Rgba32>(a,b)
      let lower = sharpDraw p1
      let upper = sharpDraw p2
      I.Mutate(fun x -> 
        x.DrawImage(lower, Point(0, 0), 1f) |> ignore
        x.DrawImage(upper, Point(0, 0), 1f) |> ignore)
      I
    | AffineTransform(p,M,c,w,h) ->
      affinetransform p M c w h
    | Scale(p,sx,sy,w,h) -> 
      let M = Matrix3x2Extensions.CreateScale(SizeF(float32 sx, float32 sy))
      affinetransform p M Color.Black w h
    | Rotate(p,cx,cy,dgr,c,w,h) ->
      let M = Matrix3x2Extensions.CreateRotation(float32 dgr, PointF(float32 cx, float32 cy))
      affinetransform p M c w h
    | Translate(p,dx,dy,c,w,h) ->
      let M = Matrix3x2Extensions.CreateTranslation(PointF(float32 dx, float32 dy))
      affinetransform p M c w h
    | Crop(p,x,y,w,h) ->
      let I = sharpDraw p
      let rect = Rectangle(Point(x,y), Size(x,y))
      I.Mutate(fun x -> x.Crop(rect)|>ignore)
      I


let p = Empty(30,50)
printfn "\nAn empty box:\n %A" p
(sharpDraw p).Save("p.jpg")
let q = Leaf((fun x y w h -> ()),50,30)
printfn "\nA full box: %A" q
(sharpDraw q).Save("q.jpg")

let r = horizontal p Top q
printfn "\nhorizontal p Top q: %A" r
let s = vertical p VPosition.Center q
printfn "\nvertical p VPosition.Center q: %A" q
let t = horizontal r Bottom s;
printfn "\nhorizontal r Bottom s: %A" t
(sharpDraw t).Save("nonTrivial.jpg")
let w,h = getSize t
let u = Rotate(t, 0, 0, 10.0*System.Math.PI/180.0, Color.LightGray, w, h)
printfn "\nRotate(t): %A" u
(sharpDraw u).Save("rotate.jpg")

let gif = sharpDraw t
let frameDelay = 100
let gifMetaData = gif.Metadata.GetGifMetadata()
gifMetaData.RepeatCount <- 5us
let metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata()
metadata.FrameDelay <- frameDelay
for i in 1 .. 59 do
  let frame = sharpDraw (Rotate(t, w/2, h/2, (float i)/60.0*2.0*System.Math.PI, Color.LightGray, w, h))
  let md = frame.Frames.RootFrame.Metadata.GetGifMetadata();
  md.FrameDelay <- frameDelay
  gif.Frames.AddFrame(frame.Frames.RootFrame) |> ignore
gif.SaveAsGif("animated.gif");

(*
let hPos = [Top; HPosition.Center; Bottom];
let lstHorisontal0 = List.map (fun pos -> horizontal p pos p) hPos
printfn "\nHorizontally empty-empty: %A" (List.zip hPos lstHorisontal0)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstHorisontal0)
let lstHorisontal1 = List.map (fun pos -> horizontal p pos q) hPos
printfn "\nHorizontally empty-full: %A" (List.zip hPos lstHorisontal1)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstHorisontal1)
let lstHorisontal2 = List.map (fun pos -> horizontal q pos p) hPos
printfn "\nHorizontally full-empty: %A" (List.zip hPos lstHorisontal2)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstHorisontal2)

let vPos = [Left; VPosition.Center; Right];
let lstVertical0 = List.map (fun pos -> vertical p pos p) vPos
printfn "\nVertically empty-empty: %A" (List.zip vPos lstVertical0)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstVertical0)
let lstVertical1 = List.map (fun pos -> vertical p pos q) vPos
printfn "\nVertically empty-full: %A" (List.zip vPos lstVertical1)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstVertical1)
let lstVertical2 = List.map (fun pos -> vertical q pos p) vPos
printfn "\nVertically full-empty: %A" (List.zip vPos lstVertical2)
List.iter (fun (v,p) -> draw p (string v)) (List.zip hPos lstVertical2)

let tPos = [0.0; 0.5; 1.0];
let lstOnTop0 = List.map (fun a -> List.map (fun b -> ontop p a b p) tPos) tPos
printfn "\nOnTop empty-empty: %A" (List.zip tPos (List.map (fun lst -> List.zip tPos lst) lstOnTop0))
List.iter (fun lst -> List.iter (fun p -> draw p "p") lst) lstOnTop0
let lstOnTop1 = List.map (fun a -> List.map (fun b -> ontop p a b q) tPos) tPos
printfn "\nOnTop empty-full: %A" (List.zip tPos (List.map (fun lst -> List.zip tPos lst) lstOnTop1))
List.iter (fun lst -> List.iter (fun p -> draw p "p") lst) lstOnTop1
let lstOnTop2 = List.map (fun a -> List.map (fun b -> ontop q a b p) tPos) tPos
printfn "\nOnTop full-empty: %A" (List.zip tPos (List.map (fun lst -> List.zip tPos lst) lstOnTop2))
List.iter (fun lst -> List.iter (fun p -> draw p "p") lst) lstOnTop2

let r = horizontal p Top q
draw r "non-trivial tree"
let s = vertical p VPosition.Center q
draw s "non-trivial tree"
let t = horizontal r Bottom s;
List.iter (fun s -> printfn "%A" (getSize s)) [r;s]
printfn "horizontal %A Bottom %A = %A" r s t
draw t "non-trivial tree"
*)
/////////////////////////////////