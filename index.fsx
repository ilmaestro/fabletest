#r "node_modules/fable-core/Fable.Core.dll"
#load "node_modules/fable-import-pixi/Fable.Import.Pixi.fs"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.Browser
open Fable.Import

module GameTypes =
    type GlobalEvent =
    | Continue
    | RequestState of string

    [<AbstractClass>]
    type StateBase() =
        abstract member Update: float -> unit
        abstract member GetEvent: unit -> GlobalEvent
        abstract member OnEnter: unit -> unit
        abstract member OnExit: unit -> unit

module App =
    /// The width of the canvas
    let width = window.innerWidth
    /// The height of the canvas
    let height = window.innerHeight
    /// device pixel ratio
    let dp = window.devicePixelRatio

    let canvas = document.createElement_canvas()
    canvas.width <- width * dp
    canvas.height <- height * dp
    canvas.style.width <- width.ToString() + "px"
    canvas.style.height <- height.ToString() + "px"

    let renderOptions = 
        createObj [
            "view" ==> canvas
            "transparent" ==> false
            "antialias" ==> true
            "autoResize" ==> false
            "resolution" ==> dp
            "backgroundColor" ==> 0xffffff
            ] :?> PIXI.RendererOptions

    let renderer = PIXI.WebGLRenderer(width, height, renderOptions)
    
    document.body.appendChild(renderer.view)

    let stage = PIXI.Container()

    let render() =
        renderer.render(stage)

module Map =
    let baseTexture = PIXI.Texture.fromImage("assets/tileset_1bit.png")
    let tilesprite = PIXI.extras.TilingSprite(baseTexture, 128., 128.)
    let tilecount = 64
    let columns = 8
    let width = 20
    let height = 20
    let tilewidth = 16
    let tileheight = 16

    let background = 
        [|
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
            [|7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7;7|];
        |]

    let obstacles =
        [|
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;16;0;44;44;14;44|];
            [|0;0;0;0;0;0;22;22;22;22;0;0;0;0;16;0;44;13;53;13|];
            [|0;0;58;57;0;0;0;0;0;0;0;0;0;0;16;0;44;44;44;44|];
            [|0;0;51;58;0;0;0;0;0;0;0;0;0;0;0;0;16;16;16;16|];
            [|0;0;0;0;3;3;0;0;3;3;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;3;0;0;0;0;3;0;0;15;0;0;0;0;0;0;0|];
            [|0;0;0;0;37;0;0;0;0;37;0;0;0;0;45;0;0;0;0;0|];
            [|0;22;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;22;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;57;0|];
            [|0;22;0;0;0;0;0;0;0;0;0;0;0;0;0;0;58;57;0;0|];
            [|0;22;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;22;22;0;0;0;0;0|];
            [|0;0;0;0;37;3;3;3;3;37;0;0;0;22;22;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;22;22;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;30;30;30;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;30;30;30;30;30;30;30;0;0;0;0;0;0;0|];
            [|30;30;30;22;22;22;30;30;30;30;30;30;30;0;0;43;35;35;35;43|];
            [|37;37;37;37;37;30;22;0;0;0;0;0;0;0;0;0;44;50;44;0|];
            [|30;37;37;37;37;30;22;0;0;0;0;43;43;43;43;43;35;35;35;43|];
            [|30;37;37;37;37;30;22;0;0;0;0;0;0;0;0;43;43;43;43;43|];
        |]

    let water =
        [|
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;9;10;10;11;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;17;18;18;19;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;17;18;18;19;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;17;18;18;19;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;17;18;18;19;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;17;18;18;19;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;25;26;26;27;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
            [|0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0|];
        |]

    let createSprite index row col =
        let srcCol = (if index % 8 = 0 then 8 else index % 8) - 1
        let srcRow = Math.Floor((float index / 8.))
        let x = (float srcCol * float tilewidth)
        let y = (float srcRow * float tileheight)
        let tile = PIXI.Texture(baseTexture, PIXI.Rectangle(x, y, float tilewidth, float tileheight))
        let sprt = PIXI.Sprite(tile)
        sprt.position.x <- col * float tilewidth
        sprt.position.y <- row * float tileheight
        sprt

    let createLayer (layer: int [] []) =
        let layerContainer = PIXI.Container()
        for col in 0 .. width - 1  do
            for row in 0 .. height - 1 do
                let index = layer.[row].[col]
                if index > 0 then
                    let sprite = createSprite index (float row) (float col)
                    layerContainer.addChild(sprite) |> ignore
                else ()
        layerContainer

module Keyboard =
    let mutable keysPressed = Set.empty
    let reset() = keysPressed <- Set.empty
    let isPressed keyCode = Set.contains keyCode keysPressed
    let update (e: KeyboardEvent, pressed) =
        let keyCode = int e.keyCode
        let op = if pressed then Set.add else Set.remove
        keysPressed <- op keyCode keysPressed
        null
    let init() =
        window.addEventListener_keydown (fun e -> update(e, true))
        window.addEventListener_keyup (fun e -> update(e, false))

module GameState =
    type T =
    | EmptyState
    | ChangeState of (string * string * GameTypes.StateBase * GameTypes.StateBase)
    | Current of string * GameTypes.StateBase

    type MainMenu() =
        inherit GameTypes.StateBase()
        let text = PIXI.Text("Press Enter to Begin.")

        override this.Update(dt: float) =
            ()

        override this.GetEvent() =
            if Keyboard.isPressed 13 then GameTypes.RequestState "ToWorld" else GameTypes.Continue

        override this.OnEnter() =
            text.anchor.x <- 0.5
            text.x <- App.width / 2.0
            text.y <- App.height / 2.0
            App.stage.addChild(text) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(text) |> ignore

    type Transition(text, timeout, nextState) =
        inherit GameTypes.StateBase()
        let text = PIXI.Text(text)
        let mutable start = 0.0
        let mutable elapsed = 0.0

        override this.Update(dt: float) =
            start <- if start = 0.0 then dt else start
            elapsed <- dt - start

        override this.GetEvent() = 
            if elapsed > timeout then GameTypes.RequestState nextState
            else GameTypes.Continue

        override this.OnEnter() =
            text.anchor.x <- 0.5
            text.x <- App.width / 2.0
            text.y <- App.height / 2.0
            App.stage.addChild(text) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(text) |> ignore

    type World() =
        inherit GameTypes.StateBase()
        let girl = PIXI.Sprite.fromImage("assets/pink_girl.png")
        let bg = Map.createLayer Map.background
        let obs = Map.createLayer Map.obstacles
        let water = Map.createLayer Map.water
        let worldContainer = PIXI.Container()

        override this.Update(dt: float) =
            girl.rotation <- girl.rotation + 0.01

        override this.GetEvent() = GameTypes.Continue
        override this.OnEnter() =
            girl.position.x <- App.width / 2.0
            girl.position.y <- App.height / 2.0
            girl.anchor.x <- 0.5
            girl.anchor.y <- 0.5

            bg.position.x <- 100.
            bg.position.y <- 100.
            obs.position.x <- 100.
            obs.position.y <- 100.
            water.position.x <- 100.
            water.position.y <- 100.
        
            worldContainer.addChild(girl) |> ignore
            worldContainer.addChild(bg) |> ignore
            worldContainer.addChild(water) |> ignore
            worldContainer.addChild(obs) |> ignore
            App.stage.addChild(worldContainer) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(worldContainer) |> ignore

module GameStateManager =
    open GameTypes
    open System.Collections.Generic

    /// TODO: change to a stack?
    let private gameStates = new Dictionary<string, StateBase>()
    let mutable currentState = GameState.EmptyState

    /// state methods
    let setupStates () =
        gameStates.Add("MainMenu", new GameState.MainMenu())
        gameStates.Add("ToWorld", new GameState.Transition("World 1-1", 2000.0, "World"))
        gameStates.Add("World", new GameState.World())

    let requestStateChange fromState toState =
        currentState <- GameState.ChangeState(fromState, toState, gameStates.Item(fromState), gameStates.Item(toState))

    let nextState name =
        let newState = gameStates.Item(name)
        newState.OnEnter()
        currentState <- GameState.Current(name, newState)

module Game = 
    open GameTypes
    open GameState

    GameStateManager.setupStates()
    Keyboard.init()

    let checkEvent fromState evt =
        /// Check for global events to handle
        match evt with
        | GameTypes.Continue -> ()
        | GameTypes.RequestState toState -> 
            GameStateManager.requestStateChange fromState toState

    let updateState() =
        match GameStateManager.currentState with
        | EmptyState ->
            GameStateManager.nextState "MainMenu"
        | Current(name, state) -> 
            checkEvent name (state.GetEvent())
        | ChangeState(fromState, toState, oldState, newState) ->
            oldState.OnExit()
            GameStateManager.nextState toState

    let update(dt:float) =
        match GameStateManager.currentState with
        | Current(name, state) -> 
            state.Update(dt)
        | _ -> ()

    let rec animate (dt:float) =
        window.requestAnimationFrame(Func<_,_> animate) |> ignore
        updateState()
        update(dt)
        App.render()
    
    let start() =
        animate(0.0)

// kick it off
Game.start()