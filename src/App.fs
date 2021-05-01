module App

open System
open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JS

type TixyFunctionType = Func<float, float, int, int, float> option

type [<AllowNullLiteral>] HTMLCanvas =
    inherit HTMLCanvasElement
    abstract style: CSSStyleDeclaration with get, set

[<Import("createTixyFunction", "../public/helper.js")>]
let createTixyFunction : code:string -> TixyFunctionType = jsNative

let examples = JsInterop.importDefault<obj> "./examples.json"

let count = 16
let size = 16.0
let spacing = 1.0
let width = (float(count) * (size + spacing)) - spacing
let halfSize = size / 2.0
let offset = halfSize

let input = document.getElementById "input" :?> HTMLInputElement
let editor = document.getElementById "editor"
let comment = document.getElementById "comment"
let output = document.getElementById "output" :?> HTMLCanvas
let context = output.getContext_2d()

let dpr =
    if window.devicePixelRatio > 0.0
    then window.devicePixelRatio
    else 1.0

let mutable startTime = DateTime.UtcNow

let mutable tixyInner : TixyFunctionType = None
let tixy (t:float) (i:float) (x:int) (y:int) =
    match tixyInner with
    | Some _ -> tixyInner.Value.Invoke(t, i, x, y)
    | None -> 0.0

let updateOutput() =
    let a = width * dpr
    output.height <- a
    output.width <- a

    context.scale(dpr, dpr)

    let sta = $"{width}px"
    output.style.height <- sta
    output.style.width <- sta

let getCode (url:URL) =
    url.searchParams.get("code")

let updateInput (code: string option) =
    if code.IsSome then input.value <- code.Value

let readURL() =
    URL.Create document.location.href
        |> getCode
        |> updateInput

let checkLength (code: string) =
    if code.Length > 32
    then editor.classList.add("over-limit")
    else editor.classList.remove("over-limit")

let updateComments (comments:string array) =
    let lines = comment.querySelectorAll("label")
    if comments.Length = 1
    then
        lines.[0].innerHTML <- "&nbsp;"
        lines.[1].innerHTML <- $"// {comments.[0]}"
    else
        lines.[0].innerHTML <- $"// {comments.[0]}"
        lines.[1].innerHTML <- $"// {comments.[1]}"

let tryFindIndex (snippets:ResizeArray<obj>) (code:string) =
    snippets |> Seq.tryFindIndex (fun x -> x |> string |> (=) code)

let updateCommentsForCode() =
    let code = input.value
    let snippets = Constructors.Object.values examples
    let comments = Constructors.Object.keys examples
    let index = tryFindIndex snippets code
    if index.IsSome then
        let newComments = comments.[index.Value].Split '\n'
        updateComments newComments

let calcIndex (i:int) (j:int) =
    float(j) * size + float(i)

let calcPoint (k:int) =
    float(k) * (size + spacing) + offset

let calcRadius (v:float) =
    let vsize = v * size
    let r = abs(vsize) / 2.0
    (if r > halfSize then halfSize else r), (if vsize >= 0.0 then "#FFF" else "#F24")

let drawArcOnCanvas (x:float) (y:float) (radius:float) (color:string) = 
    context.beginPath()
    context.fillStyle <- U3.Case1 color
    context.arc(x, y, radius, 0.0, Math.PI * 2.0)
    context.fill()

let drawOnCanvas() =
    let time = (DateTime.UtcNow - startTime).TotalMilliseconds / 1000.0
    for j in 0 .. count do
        for i in 0 .. count do
            let index = calcIndex i j
            let value = tixy time index i j

            let x = calcPoint i
            let y = calcPoint j
            let radius, color = calcRadius value

            drawArcOnCanvas x y radius color

let updateTixyInner() =
    startTime <- DateTime.UtcNow
    let code = input.value
    checkLength(code)
    tixyInner <- createTixyFunction(code)

let nextExample() =
    let code = input.value
    let snippets = Constructors.Object.values examples
    let idx = tryFindIndex snippets code
    let newIndex = if idx.IsNone || idx.Value + 1 >= snippets.Count
                   then 0
                   else idx.Value + 1
    let newCode = snippets.[newIndex]
    input.value <- newCode |> string

    updateCommentsForCode()
    updateTixyInner()

let updateURL (e: Event) =
    e.preventDefault()
    let code = input.value
    let url = URL.Create document.location.href
    url.searchParams.set("code", code)
    history.replaceState(null, code, url.href)

let rec render _ =
    let code = input.value
    checkLength(code)

    if tixyInner.IsSome
    then
        updateOutput()
        drawOnCanvas()

    window.requestAnimationFrame(render) |> ignore


input.addEventListener("input", fun _ -> updateTixyInner())
input.addEventListener("blur", fun _ -> editor.classList.remove("focus"))
input.addEventListener(
    "focus",
    fun _ ->
        editor.classList.add("focus")
        updateComments([|
            "hit \"enter\" to save in URL";
            "or get <a href=\"https://twitter.com/aemkei/status/1323399877611708416\">more info here</a>"
          |]))

editor.addEventListener("submit", updateURL)

window.onpopstate <- fun _ ->
    readURL()
    updateTixyInner()

output.addEventListener("click", fun _ -> nextExample())


updateOutput()
updateTixyInner()
render 0.
