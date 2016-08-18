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

    [<Measure>] type HP // Hit Points

    type Position = { x: float; y: float }
    type Location = { row: int32; col: int32 }
    type Rectangle = { position: Position; width: float; height: float }
    type TextureMap = { texture: PIXI.Texture; rowSize: int32; colSize: int32; tilewidth: float; tileheight: float }

    [<AbstractClass>]
    type StateBase() =
        abstract member Update: float -> unit
        abstract member GetEvent: unit -> GlobalEvent
        abstract member OnEnter: unit -> unit
        abstract member OnExit: unit -> unit

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

    let right = 39
    let up = 38
    let down = 40
    let left = 37
    let enter = 13

module GameHelpers =
    open GameTypes

    let positionToPoint position = PIXI.Point(position.x, position.y)
    let addChildToContainer (container: PIXI.Container) (displayObject: PIXI.DisplayObject) = container.addChild(displayObject) |> ignore

    let getMapPosition location textureMap =
        { x = (float location.col) * textureMap.tilewidth; y = (float location.row) * textureMap.tileheight; }

    let getFrame index texture =
        let location = {
            col = (if index % texture.colSize = 0 then texture.colSize else index % texture.colSize) - 1;
            row = int (Math.Floor((float index / float texture.rowSize)));
        }
        let position = getMapPosition location texture
        { position = position; width = float texture.tilewidth; height = float texture.tileheight }

    let getSpriteFromTexture texture index =
        let frame = getFrame index texture
        let tile = PIXI.Texture(texture.texture, PIXI.Rectangle(frame.position.x, frame.position.y, frame.width, frame.height))
        PIXI.Sprite(tile)

    let isLocationOnMap (map: int [][]) location =
        location.row >= 0 &&
        location.col >= 0 &&
        location.row < map.Length &&
        location.col < map.[location.row].Length

    let getTileIndex (map: int [][]) location =
        map.[location.row].[location.col]

    let isTileIndex (map: int [][]) location index =
        (getTileIndex map location) = index

    let debounce timeout =
        let mutable time = 0.0
        let doBounce dt =
            if dt - time > timeout then
                time <- dt
                true
            else false
        doBounce 
    
    let getDirection() = (
        (if Keyboard.isPressed Keyboard.up then -1 else 0),
        (if Keyboard.isPressed Keyboard.down then 1 else 0),
        (if Keyboard.isPressed Keyboard.right then 1 else 0), 
        (if Keyboard.isPressed Keyboard.left then -1 else 0)
        )

module App =
    /// The width of the canvas
    let width = window.innerWidth - 100.
    /// The height of the canvas
    let height = window.innerHeight - 100.
    /// device pixel ratio
    let dp = window.devicePixelRatio

    let canvas = document.createElement_canvas()
    canvas.width <- width * dp
    canvas.height <- height * dp
    canvas.style.width <- width.ToString() + "px"
    canvas.style.height <- height.ToString() + "px"

    let private renderOptions = 
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
    open GameTypes
    open GameHelpers

    let renderMapLayer textureMap width height (layer: int [][]) =
            let layerContainer = PIXI.Container()
            let addToLayer = addChildToContainer layerContainer
            let sprite = getSpriteFromTexture textureMap
            let setPos location (sprite: PIXI.Sprite) =
                sprite.position <- (getMapPosition location textureMap) |> positionToPoint
                sprite.anchor <- {x = 0.5; y = 0.5;} |> positionToPoint
                sprite
        
            for col in 0 .. width - 1  do
                for row in 0 .. height - 1 do
                    let index = layer.[row].[col]
                    if index > 0 then
                        (sprite index) 
                        |> setPos {row = row; col = col;}
                        |> addToLayer
                    else ()
            layerContainer

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

    let tilemap1bit = { texture = PIXI.Texture.fromImage("assets/tileset_1bit.png"); rowSize = 8; colSize = 8; tilewidth = 16.; tileheight = 16.; }
    let mapLayers1bit = 
        [background; water; obstacles; ]
        |> List.map (renderMapLayer tilemap1bit 20 20)

module Character =
    open GameTypes
    open GameHelpers

    type PlayerMove =
    | Move of Location
    | Blocked
    | ReadSign of Location

    type Player(textureMap, textureIndex, location, initialHP, obstacles) =
        let sprite = getSpriteFromTexture textureMap textureIndex
        let mutable hp = initialHP
        let mutable location = location
        let canMove location =
            isLocationOnMap obstacles location && isTileIndex obstacles location 0 
        let isSign location =
            isLocationOnMap obstacles location && isTileIndex obstacles location 15
        do
            sprite.position <- getMapPosition location textureMap |> positionToPoint
            sprite.anchor <- {x = 0.5; y = 0.5;} |> positionToPoint
        member this.Sprite = sprite
        member this.CheckMove(dx, dy) =
            let newLocation = { location with row = location.row + dy; col = location.col + dx; }
            if canMove newLocation then Move(newLocation)
            else if isSign newLocation then ReadSign(newLocation)
            else Blocked
        member this.Move(newLocation) =
            location <- newLocation
            sprite.position <- getMapPosition location textureMap |> positionToPoint

module GameState =
    type T =
    | EmptyState
    | ChangeState of (string * string * GameTypes.StateBase * GameTypes.StateBase)
    | Current of string * GameTypes.StateBase

    type MainMenu() =
        inherit GameTypes.StateBase()
        let text = PIXI.Text("Press Enter to Begin.")
        do
            text.anchor.x <- 0.5
            text.x <- App.width / 2.0
            text.y <- App.height / 2.0
        override this.Update(dt: float) =
            ()

        override this.GetEvent() =
            if Keyboard.isPressed Keyboard.enter then GameTypes.RequestState "ToWorld" else GameTypes.Continue

        override this.OnEnter() =
            App.stage.addChild(text) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(text) |> ignore

    type ReadSign() =
        inherit GameTypes.StateBase()
        let text = PIXI.Text("Sign says: Hello")
        do
            text.anchor.x <- 0.5
            text.x <- App.width / 2.0
            text.y <- App.height / 2.0
        override this.Update(dt: float) =
            ()

        override this.GetEvent() =
            if Keyboard.isPressed Keyboard.enter then GameTypes.RequestState "World" else GameTypes.Continue

        override this.OnEnter() =
            App.stage.addChild(text) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(text) |> ignore

    type Transition(text, timeout, nextState) =
        inherit GameTypes.StateBase()
        let text = PIXI.Text(text)
        let mutable start = 0.0
        let mutable elapsed = 0.0
        do
            text.anchor.x <- 0.5
            text.x <- App.width / 2.0
            text.y <- App.height / 2.0

        override this.Update(dt: float) =
            start <- if start = 0.0 then dt else start
            elapsed <- dt - start

        override this.GetEvent() = 
            if elapsed > timeout then GameTypes.RequestState nextState
            else GameTypes.Continue

        override this.OnEnter() =
            start <- 0.0
            App.stage.addChild(text) |> ignore
    
        override this.OnExit() =
            App.stage.removeChild(text) |> ignore

    type World() =
        inherit GameTypes.StateBase()

        let player = new Character.Player(Map.tilemap1bit, 45, { row = 1; col = 3}, 12<GameTypes.HP>, Map.obstacles)
        let worldContainer = PIXI.Container()
        let playerDebounce = GameHelpers.debounce 200.0
        let mutable worldState = GameTypes.Continue
        do
            Map.mapLayers1bit |> List.iter (fun layer ->
                layer.position.x <- 0.
                layer.position.y <- 0.
                worldContainer.addChild(layer) |> ignore
                )

            worldContainer.addChild(player.Sprite) |> ignore

        override this.Update(dt: float) =
            player.Sprite.rotation <- player.Sprite.rotation + 0.01

            if playerDebounce dt then
                match Keyboard.getDirection() with
                | (0,0,0,0) -> ()
                | (up, down, right, left) -> 
                    let dx = right + left
                    let dy = down + up
                    match player.CheckMove(dx, dy) with
                    | Character.Move location -> player.Move(location)
                    | Character.Blocked -> ()
                    | Character.ReadSign location ->
                        worldState <- GameTypes.RequestState "ReadSign" 
                        ()

        override this.GetEvent() = worldState
        override this.OnEnter() =
            worldState <- GameTypes.Continue
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
        gameStates.Add("ReadSign", new GameState.ReadSign())

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