module ImgUtil

// colors
type color
val red       : color
val blue      : color
val green     : color
val yellow    : color
val fromRgb   : int * int * int -> color
val fromArgb  : int * int * int * int -> color
val fromColor : color -> int * int * int * int

// canvas
type canvas
val mk          : int -> int -> canvas
val init        : int -> int -> (int*int -> color) -> canvas
val setPixel    : color -> int * int -> canvas -> unit
val getPixel    : canvas -> int * int -> color
val setLine     : color -> int * int -> int * int -> canvas -> unit
val setBox      : color -> int * int -> int * int -> canvas -> unit
val setFillBox  : color -> int * int -> int * int -> canvas -> unit
val width       : canvas -> int
val height      : canvas -> int
val scale       : canvas -> int -> int -> canvas

// load a canvas from an image file (e.g., a png-file)
val fromFile  : string -> canvas

// save a canvas as a png file
val toPngFile : string -> canvas -> unit

// show canvas in a gui
val show      : string -> canvas -> unit

// start a simple app
val runSimpleApp : string -> int -> int
                -> (int -> int -> canvas)
                -> unit

// Simplification of SDL keys used for keypress/keydown/keyup events
type key = Keysym of int
// Wrappertype for supported keys in ImgUtil, for easier/prettier pattern-matching
type ImgUtilKey =
    | Unknown
    | DownArrow
    | UpArrow
    | LeftArrow
    | RightArrow
    | Space

/// Convert a `key : Keysym of int` into an `ImgUtilKey`
/// Not pretty, but "good enough"
val getKey : key -> ImgUtilKey

// start an app that can listen to key-events
val runApp    : string -> int -> int
             -> (int -> int -> 's -> canvas)
             -> ('s -> key -> 's option)
             -> 's -> unit
