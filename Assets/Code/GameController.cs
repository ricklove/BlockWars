using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public bool showTitleScreen = true;
    public bool hideFactory = true;

    internal List<GameObject> blockPrefabs;
    internal List<Texture2D> blockImages;

    // NOT USED - SET IN DESIGNER!!!!!
    //public float shooterPower = 200;
    public float shooterMaxLength = 3;
    public float shooterVelocity = 50;
    public float shootingBlockMassScale = 1.5f;
    public bool shooterUsesSameSpeed = true;
    public float shooterRadiusFromBase = 5.0f;

    public float blockMaxDistanceFromBase = 10.0f;
    public float blockHitBaseDistance = 3.0f;

    private float shooterStageSizeModifier;

    public float baseDistance = 20.0f;

    public float endOfTurnTimeSpan = 5;
    public int hitBaseIndex = -1;

    public GameState gameState { get; private set; }

    private GUIController guiController;
    private EditorController editorController;
    private FileController fileController;
    private MouseHelper mouseHelper;

    private GameObject baseHolder;
    private GameObject factory;
    private GameObject prefabBase;

    private GameObject activeBaseMarker;

    private GameObject cameraObj;
    private Camera gameCamera;
    private GameObject radarCameraObj;
    private Camera radarCamera;

    private GameObject shooter;
    private GameObject grabbedBlock;
    private GameObject shootingBlock;

    private List<PlayerInfo> players;
    private int playerTurnIndex;
    private TurnState _turnState;
    private TurnState turnState
    {
        get { return _turnState; }
        set
        {
            _turnState = value;
            hasCameraMovedForTurnState = false;
            Debug.Log("TurnState=" + value);
        }
    }

    private bool hasCameraMovedForTurnState = false;
    private NewGameOptions newGameOptions;

    void Start()
    {
        InitializeScene();
    }

    void InitializeScene()
    {
        // Remove any created items from last game and reset variables
        if (players != null)
        {
            foreach (var p in players)
            {
                Destroy(p.baseObject);
            }

            players = null;
        }

        playerTurnIndex = 0;
        turnState = TurnState.Starting;

        // Initialize references
        guiController = GetComponent<GUIController>();
        editorController = GetComponent<EditorController>();
        fileController = GetComponent<FileController>();

        // Add back editorController if it is not there (it was destoryed for performance )
        if (editorController == null)
        {
            editorController = gameObject.AddComponent<EditorController>();
        }

        blockPrefabs = new List<GameObject>();
        blockPrefabs.Add(GameObject.Find("PB_Square"));
        blockPrefabs.Add(GameObject.Find("PB_Hexagon"));
        blockPrefabs.Add(GameObject.Find("PB_Trapezoid"));
        blockPrefabs.Add(GameObject.Find("PB_Rhombus"));
        blockPrefabs.Add(GameObject.Find("PB_Triangle"));
        blockPrefabs.Add(GameObject.Find("PB_ThinRhombus"));

        blockImages = new List<Texture2D>();
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatSquare"));
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatHexagon"));
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatTrapezoid"));
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatRhombus"));
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatTriangle"));
        blockImages.Add((Texture2D)Resources.Load("Texture/FlatThinRhombus"));

        mouseHelper = GetComponent<MouseHelper>();

        baseHolder = GameObject.Find("BaseHolder");
        factory = GameObject.Find("Factory");
        prefabBase = GameObject.Find("PB_Base");

        activeBaseMarker = GameObject.Find("ActiveBaseMarker");

        cameraObj = GameObject.Find("GameCamera");
        gameCamera = cameraObj.camera;
        radarCameraObj = GameObject.Find("RadarCamera");
        radarCamera = radarCameraObj.camera;
        SetRadarCameraVisible(false);

        shooter = GameObject.Find("Shooter");

        // Hide Factory
        if (hideFactory)
        {
            factory.transform.position = new Vector3(0, 100, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Handle 'esc' button for quit
        if (Input.GetKey("escape"))
        {
            gameState = GameState.Closing;
        }

        // Ensure sound keeps looping - Sometimes it stops randomly
        if (!audio.isPlaying)
        {
            audio.Play();
            Debug.Log("Sound was restarted!!");
        }

        if (gameState == GameState.Starting)
        {
            // Show Title
            if (!showTitleScreen ||
              guiController.ShowTitleScreen(3.0f) == GUIResult.Finished)
            {
                gameState = GameState.MainMenu;
            }
        }

        if (gameState == GameState.MainMenu)
        {
            var result = guiController.ShowMainMenu();

            if (result.isFinished)
            {
                switch (result.mainMenuAction)
                {
                    case MainMenuAction.NewGame:
                        gameState = GameState.LoadingNewGame;
                        newGameOptions = result.newGameOptions;
                        break;
                    case MainMenuAction.Close:
                    default:
                        gameState = GameState.Closing;
                        break;
                }
            }
        }

        if (gameState == GameState.LoadingNewGame)
        {
            // TEMP: Refactor this
            guiController.ShowNone();

            var options = newGameOptions;

            var humans = new List<int>();

            for (int i = 0; i < options.humanPlayerCount; i++)
            {
                humans.Add(i);
            }

            PerformanceController.PausePerformance();
            NewGame(options.humanPlayerCount + options.computerPlayerCount, humans);
            PerformanceController.ResetPerformance(0.5f);

            gameState = GameState.Editor;
        }

        if (gameState == GameState.Editor)
        {
            HandleCameraInputs();

            // Handle when editor is finished
            if (editorController.editorState == EditorState.Finished || !newGameOptions.shouldShowEditor)
            {
                editorController.FinishedIsHandled();

                PerformanceController.PausePerformance();

                // Disable editor for performance
                Destroy(editorController);

                CreateBasesIfEmpty();

                RemoveAllAligners();
                EnableBases();

                PerformanceController.ResetPerformance(1.0f);

                // Start Game Mode
                gameState = GameState.Playing;
            }
        }

        if (gameState == GameState.Playing)
        {
            SetRadarCameraVisible(true);

            var hasMoved = HandleCameraInputs();

            if (hasMoved)
            {
                hasCameraMovedForTurnState = true;
            }

            if (turnState == TurnState.Starting)
            {
                // Move camera
                var pos = players[playerTurnIndex].baseObject.transform.position;
                mouseHelper.MoveCameraToLookAtGroundPoint(pos.x, pos.z);

                // Move the ActiveBaseMarker
                activeBaseMarker.transform.position = pos;

                // TODO: Display Player Info
                Debug.Log("Turn = PlayerIndex " + playerTurnIndex);

                turnState = TurnState.Aiming;

                hitBaseIndex = -1;
            }

            if (turnState == TurnState.Aiming)
            {
                GameAction gameAction;

                if (players[playerTurnIndex].isHuman)
                {
                    // TODO: Allow player to shoot blocks only near base
                    gameAction = HandleGameInputs();
                }
                else
                {
                    // Computer Aiming
                    gameAction = ComputerAim();
                }

                if (gameAction == GameAction.PlayerShotBlock)
                {
                    turnState = TurnState.Shooting;
                }
                else if (gameAction == GameAction.None)
                {
                    // Do Nothing
                }
                else
                {
                    throw new System.Exception("Unknown GameAction: " + gameAction);
                }

            }

            if (turnState == TurnState.Shooting)
            {
                // Detect shooting block stop movement + Timeout
                var sController = shootingBlock.GetComponent<ShootingBlockController>();
                var isTurnFinished = sController.shootingBlockState == ShootingBlockState.Finished &&
                  Time.time - sController.finishedTime > endOfTurnTimeSpan;

                // Detect if has hit enemy base
                var pos = shootingBlock.transform.position;

                for (var iPlayer = 0; iPlayer < players.Count; iPlayer++)
                {
                    if (iPlayer == playerTurnIndex)
                    {
                        continue;
                    }

                    var player = players[iPlayer];
                    var pBase = player.baseObject;

                    if (!player.isAlive)
                    {
                        continue;
                    }

                    var bPos = pBase.transform.position;
                    var distance = Vector3.Distance(bPos, pos);
                    if (distance < blockHitBaseDistance)
                    {
                        hitBaseIndex = iPlayer;
                    }
                }


                if (sController.shootingBlockState != ShootingBlockState.Finished &&
                  !hasCameraMovedForTurnState)
                {
                    if (hitBaseIndex < 0)
                    {
                        // Follow the shooting block with the camera (while it is still moving)
                        mouseHelper.MoveCameraToLookAtGroundPoint(pos.x, pos.z);
                    }
                    else
                    {
                        // Move camera to base
                        var pBasePos = players[hitBaseIndex].baseObject.transform.position;
                        mouseHelper.MoveCameraToLookAtGroundPoint(pBasePos.x, pBasePos.z);
                    }
                }

                if (isTurnFinished)
                {
                    sController.FinishedIsHandled();
                    turnState = TurnState.Ended;
                }
            }

            if (turnState == TurnState.Ended)
            {
                // Detect End of Game (if only one human player is still active or all human players are inactive)
                // Or if no human players - then only one computer left
                var humanPlayerCount = 0;
                var humansAliveCount = 0;
                var computersAliveCount = 0;

                foreach (var p in players)
                {
                    if (p.isHuman)
                    {
                        humanPlayerCount++;

                        if (p.isAlive)
                        {
                            humansAliveCount++;
                        }
                    }
                    else
                    {
                        if (p.isAlive)
                        {
                            computersAliveCount++;
                        }
                    }
                }

                var playersAliveCount = humansAliveCount + computersAliveCount;

                if ((humansAliveCount == 0 && humanPlayerCount > 0) || playersAliveCount <= 1)
                {
                    Debug.Log("Players -> GameOver");
                    gameState = GameState.EndOfGameReport;
                }

                // Go to next alive player
                var origPlayer = playerTurnIndex;

                do
                {
                    playerTurnIndex++;

                    if (playerTurnIndex >= players.Count)
                    {
                        playerTurnIndex = 0;
                    }

                    if (playerTurnIndex == origPlayer)
                    {
                        // Game Over (only one player alive (or maybe none alive)) - This should be handled by above check
                        break;
                    }

                } while (!players[playerTurnIndex].isAlive);

                turnState = TurnState.Starting;
            }
        }

        if (gameState == GameState.EndOfGameReport)
        {
            SetRadarCameraVisible(false);

            var winnerName = "Nobody";
            var isWinnerHuman = false;
            var loserHumans = new List<string>();

            foreach (var p in players)
            {
                if (p.isAlive)
                {
                    winnerName = p.name;
                    isWinnerHuman = p.isHuman;
                }
                else
                {
                    if (p.isHuman)
                    {
                        loserHumans.Add(p.name);
                    }
                }

            }

            var reportInfo = new EndOfGameInfo(winnerName, isWinnerHuman, loserHumans.ToArray());

            if (guiController.ShowEndOfGameReport(reportInfo, 5.0f) == GUIResult.Finished)
            {
                gameState = GameState.MainMenu;
            }
        }

        if (gameState == GameState.Closing)
        {
            Application.Quit();
        }
    }

    void NewGame(int playerCount, List<int> humanPlayers)
    {
        // TODO: Separate Root Scene with the actual level
        //Application.LoadLevel( "MainScene" );

        Debug.Log("NEW GAME!!!");

        InitializeScene();

        // Create Players & Bases
        playerTurnIndex = 0;
        turnState = TurnState.Starting;

        players = new List<PlayerInfo>();

        var iHuman = 0;
        var iComputer = 0;

        var radiansBetweenBases = 2 * Mathf.PI / playerCount;

        // n = spaces = players
        // baseDistance = 1/n * C == 1/n D PI = 1/n 2PI r = r 2PI / n
        // r = baseDistance / (2PI/n)
        // r = baseDistance * n / 2PI
        var stageRadius = baseDistance * playerCount / (2 * Mathf.PI);

        // For small number players, this can happen (less than 4)
        if (stageRadius < baseDistance / 2)
        {
            stageRadius = baseDistance / 2;
        }

        // Increase power for large radiuses
        shooterStageSizeModifier = Mathf.Sqrt(stageRadius * 2 / baseDistance);

        Debug.Log("StageRadius=" + stageRadius);

        for (int i = 0; i < playerCount; i++)
        {
            var player1Point = 1.0f * Mathf.PI;
            var radians = player1Point - (radiansBetweenBases * i);
            radians = (radians + 2 * Mathf.PI) % (2 * Mathf.PI);

            var x = stageRadius * Mathf.Cos(radians);
            var z = stageRadius * Mathf.Sin(radians);

            //if (radians > Mathf.PI)
            //{
            //  x = -x;
            //}

            //if ((radians > Mathf.PI * 0.5f)
            //  && (radians < Mathf.PI * 1.5f))
            //{
            //  z = -z;
            //}

            Debug.Log((radians / Mathf.PI) + " * PI @ " + x + " , " + z);

            var position = new Vector3(x, 0, z);
            var baseObj = (GameObject)Instantiate(prefabBase, position, Quaternion.identity);
            baseObj.tag = "Base";
            baseObj.transform.parent = baseHolder.transform;
            var isHuman = humanPlayers.Contains(i);
            var name = "";

            if (isHuman)
            {
                if (humanPlayers.Count > 1)
                {
                    name = "Player " + (iHuman + 1);
                }
                else
                {
                    name = "Player";
                }
            }
            else
            {
                if (playerCount - humanPlayers.Count > 1)
                {
                    name = "Computer " + (iComputer + 1);
                }
                else
                {
                    name = "Computer";
                }
            }

            var player = new PlayerInfo(baseObj, isHuman, name);
            players.Add(player);

            if (isHuman)
            {
                iHuman++;
            }
            else
            {
                iComputer++;
            }
        }

        // Go to editor
        // TODO: Give each human a chance to create base
        editorController.ShowEditor(players[0].baseObject, blockMaxDistanceFromBase, blockPrefabs.ToArray(), blockImages.ToArray());
    }

    void CreateBasesIfEmpty()
    {
        // Get target mass from 1st base
        float? targetMass = null;
        if (players[0].baseObject.GetComponent<BaseController>().isTouchingBlock)
        {
            targetMass = fileController.GetGameBaseState(players[0].baseObject, blockMaxDistanceFromBase).GetMass();
        }

        var baseStates = new List<BaseState>();

        // Load base states
        baseStates.AddRange(fileController.LoadBaseGroupFile("BaseData/BossBases").baseStates);
        baseStates.AddRange(fileController.LoadUserBaseGroup().baseStates);

        // Choose baseStates near target mass (increasing scope of search as needed)
        if (targetMass.HasValue)
        {
            var playersWithoutBaseCount = 0;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player.baseObject.GetComponent<BaseController>().isTouchingBlock)
                {
                    playersWithoutBaseCount++;
                }
            }

            var basesWithNearMass = new List<BaseState>();
            var error = 0.1f;

            while (basesWithNearMass.Count < playersWithoutBaseCount && error < 0.9f)
            {
                foreach (var bState in baseStates)
                {
                    var bMass = bState.GetMass();

                    if (bMass > targetMass.Value * (1.0f - error) && bMass < targetMass.Value * (1.0f + error))
                    {
                        if (!basesWithNearMass.Contains(bState))
                        {
                            basesWithNearMass.Add(bState);
                        }
                    }
                }

                Debug.Log("Creating bases for (" + playersWithoutBaseCount + "): TargetMass=" + targetMass.Value + " +- " + error * 100 + "%");

                error += 0.1f;
            }

            if (basesWithNearMass.Count > 0)
            {
                baseStates = basesWithNearMass;
            }
        }

        Debug.Log("Creating bases: BaseStates.Count=" + baseStates.Count);

        // Select the needed bases
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            if (!player.baseObject.GetComponent<BaseController>().isTouchingBlock)
            {
                // Choose a random base
                var baseI = Random.Range(0, baseStates.Count);
                var baseState = baseStates[baseI];

                fileController.SetGameBaseState(blockPrefabs.ToArray(), player.baseObject, baseState, Consts.gameLayerNumber, blockMaxDistanceFromBase);
            }
        }
    }

    void RemoveAllAligners()
    {
        // TODO: Improve the aligners performance during build
        var aligners = gameObject.GetComponentsInChildren<AlignerController>();

        foreach (var aligner in aligners)
        {
            if (aligner.gameObject.tag == "Aligner")
            {
                Destroy(aligner.gameObject);
            }
        }

        // Remove rigid bodies
        var bases = gameObject.GetComponentsInChildren<BaseController>();

        foreach (var bCon in bases)
        {
            foreach (Transform b in bCon.gameObject.transform)
            {
                if (b.gameObject.tag == "Block")
                {
                    b.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                }
            }
        }
    }

    void EnableBases()
    {
        Debug.Log("Enabling Bases");

        foreach (BaseController BaseController in gameObject.GetComponentsInChildren<BaseController>())
        {
            BaseController.MakeActive();
        }
    }

    void SetRadarCameraVisible(bool isVisible)
    {
        //Debug.Log("RadarCameraVisible=" + isVisible);

        if (isVisible)
        {
            if (radarCamera.depth != 10)
            {
                Debug.Log("RadarCamera ON");
                radarCamera.depth = 10;
            }
        }
        else
        {
            if (radarCamera.depth != -100)
            {
                Debug.Log("RadarCamera OFF");
                radarCamera.depth = -100;
            }
        }
    }


    bool HandleCameraInputs()
    {
        var hasMoved = false;
        // Simple camera movement
        // Move character
        var speed = 0.2f;
        var yModForSpeed = 0.1f;
        var zoomRatio = 0.2f;

        // Keyboard Move
        var horAxis = Input.GetAxis("Horizontal");
        var verAxis = Input.GetAxis("Vertical");

        if (horAxis != 0 || verAxis != 0)
        {
            var pos = cameraObj.transform.position;

            // Move by a ratio of the current zoom (exponential zoom)
            var speedForZoom = pos.y * yModForSpeed * speed;
            cameraObj.transform.position = new Vector3(pos.x + horAxis * speedForZoom, pos.y, pos.z + verAxis * speedForZoom);
            hasMoved = true;
        }

        // Mouse or Keyboard Scroll
        var scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.Equals))
        {
            scroll = 0.1f;
        }
        else if (Input.GetKey(KeyCode.Minus))
        {
            scroll = -0.1f;
        }

        if (scroll != 0)
        {
            // TODO: Use ray casting to determine how to zoom at a different angle
            var pos = cameraObj.transform.position;

            // Zoom by a ratio of the current zoom (exponential zoom)
            var change = pos.y * scroll * zoomRatio;
            cameraObj.transform.position = new Vector3(pos.x, pos.y - change, pos.z + change / 2.0f);
            hasMoved = true;
        }

        // Touch Input
        if (grabbedBlock == null && gameState == GameState.Playing)
        {
            if (Input.touchCount == 1)
            {
                var touch0 = Input.GetTouch(0);

                // Move
                if (touch0.phase == TouchPhase.Moved)
                {
                    var pos = cameraObj.transform.position;
                    var change = touch0.deltaPosition;
                    cameraObj.transform.position = new Vector3(pos.x + change.x * -0.03f, pos.y, pos.z + verAxis + change.y * -0.03f);
                }
            }
            else if (Input.touchCount > 1)
            {
                var touch0 = Input.GetTouch(0);
                var touch1 = Input.GetTouch(1);

                // Zoom

            }
        }

        return hasMoved;
    }

    GameAction HandleGameInputs()
    {
        var result = GameAction.None;

        if (Input.GetButtonDown("Fire1"))
        {
            var mPos = Input.mousePosition;
            Ray ray = gameCamera.ScreenPointToRay(mPos);
            RaycastHit hit;

            // Anything but the editorHeightPlane & ignoreRayCast layer
            var layerMask = 1 << Consts.mouseHeightPlaneLayerNumber;
            var layerMask2 = 1 << Consts.ignoreRaycastLayerNumber;
            layerMask = layerMask | layerMask2;

            layerMask = ~layerMask;

            var centerScreenPoint = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 5));

            GameObject hitObj = null;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                Debug.DrawLine(centerScreenPoint, hit.point, Color.red, 3);

                hitObj = hit.collider.gameObject;

                // Get Block Object (which is a parent of the block mesh or the aligners)
                hitObj = hitObj.transform.parent.gameObject;

                Debug.Log("Hit: " + hitObj.name + " - " + hitObj.tag);

                if (hitObj.tag == "Block")
                {
                    Debug.DrawLine(centerScreenPoint, hit.point, Color.yellow, 3);

                    var diff = hitObj.transform.position - players[playerTurnIndex].baseObject.transform.position;

                    if (diff.magnitude <= shooterRadiusFromBase)
                    {
                        Debug.DrawLine(centerScreenPoint, hit.point, Color.green, 3);
                        grabbedBlock = hitObj;
                    }
                }
            }
        }

        if (Input.GetButton("Fire1"))
        {
            if (grabbedBlock != null)
            {
                var mPos = mouseHelper.GetMousePosition(grabbedBlock.transform.position.y);
                SetShooterPosition(grabbedBlock, mPos);
            }
        }

        if (Input.GetButtonUp("Fire1"))
        {
            if (grabbedBlock != null)
            {
                ShootBlock(grabbedBlock);
                grabbedBlock = null;
                result = GameAction.PlayerShotBlock;
            }
        }

        return result;
    }

    void SetShooterPosition(GameObject block, Vector3 pullBackPos)
    {
        // Pull back
        var centerScreenPoint = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 5));
        Debug.DrawLine(centerScreenPoint, block.transform.position, Color.green, 3);

        // TODO: Only allow the shooter to go back so far
        // Put the shooter on the mouse at block height
        Debug.DrawLine(block.transform.position, pullBackPos, Color.blue, 3);

        var diff = pullBackPos - block.transform.position;
        // Give a slight up angle so it won't ram the ground
        diff = new Vector3(diff.x, -0.05f, diff.z);

        if (diff.sqrMagnitude > shooterMaxLength * shooterMaxLength)
        {
            diff = diff.normalized * shooterMaxLength;
        }

        shooter.transform.position = block.transform.position + diff;
    }

    void ShootBlock(GameObject block)
    {
        // Add shooting controller to the block
        var wm = block.AddComponent<ShootingBlockController>();
        wm.scale = shootingBlockMassScale;

        // Shoot block
        var rigidbody = block.GetComponent<Rigidbody>();

        var forceDirection = block.transform.position - shooter.transform.position;
        //var force = forceDirection * shooterPowerForSize;

        //if (shooterUsesSameSpeed)
        //{
        //  force = force * block.rigidbody.mass * shootingBlockMassScale;
        //}

        // Instead of force, change velocity directly
        //rigidbody.AddForce(force);
        var velocity = forceDirection.normalized * shooterVelocity * shooterStageSizeModifier;
        rigidbody.velocity = velocity;

        // Play on the shooting block
        shootingBlock = block;
        shootingBlock.audio.PlayOneShot(shooter.audio.clip, 1);
        Debug.Log("Audio for Shooter: " + shooter.audio.clip + " - " + shooter.audio.clip.name);

        // Hide shooter
        shooter.transform.position = new Vector3(0, 1000, 0);

        // Indicate action
        Debug.Log("BAM! Velocity=" + velocity);
        //Debug.Log("BAM! Force=" + force.magnitude);
    }

    private GameObject computerAimBlock;
    private float computerAimStartTime;
    private Vector3 computerAimTargetPos;
    private Vector3 errorComputerAimTargetPos;
    GameAction ComputerAim()
    {
        float maxErrorFromTarget = 7;
        float computerAimTimeSpan = 2.0f;

        if (computerAimBlock == null)
        {
            // Grab a block near the base
            var player = players[playerTurnIndex];
            var basePos = player.baseObject.transform.position;
            var radius = shooterRadiusFromBase;

            var liveEnemies = new List<PlayerInfo>();
            foreach (var p in players)
            {
                if (p != player && p.isAlive)
                {
                    liveEnemies.Add(p);
                }
            }

            var targetIndex = Random.Range(0, liveEnemies.Count - 1);
            var targetBase = liveEnemies[targetIndex].baseObject;
            var targetPos = targetBase.transform.position;


            Collider[] colliders = Physics.OverlapSphere(basePos, radius);

            // Get the block nearest the target (that is not touching the base trigger)
            GameObject nearestBlockSoFar = null;
            float sqrDistance = float.MaxValue;

            foreach (Collider hit in colliders)
            {
                var hitObj = hit.transform.parent.gameObject;

                if (hitObj.tag == "Block")
                {
                    var diff = targetPos - hitObj.transform.position;

                    // Guarentee a pick
                    if (nearestBlockSoFar == null)
                    {
                        nearestBlockSoFar = hitObj;
                        sqrDistance = diff.sqrMagnitude;
                    }

                    if (sqrDistance > diff.sqrMagnitude)
                    {
                        // TODO: Check for own base trigger contact
                        var isTouchingOwnBaseTrigger = false;

                        if (!isTouchingOwnBaseTrigger)
                        {
                            nearestBlockSoFar = hitObj;
                            sqrDistance = diff.sqrMagnitude;
                        }
                    }
                }
            }

            computerAimBlock = nearestBlockSoFar;
            computerAimStartTime = Time.time;
            computerAimTargetPos = targetPos;
            errorComputerAimTargetPos = Vector3.zero;
        }

        // Aim the block towards enemy base with error
        var aimBlock = computerAimBlock;
        var aimPos = aimBlock.transform.position;

        if (errorComputerAimTargetPos == Vector3.zero)
        {
            float errorRatio = Random.Range(-100, 101);
            errorRatio = errorRatio / 100.0f;
            errorRatio = errorRatio * errorRatio * errorRatio;

            float errorRatioB = Random.Range(-100, 101);
            errorRatioB = errorRatio / 100.0f;
            errorRatioB = errorRatioB * errorRatioB * errorRatioB;

            errorComputerAimTargetPos = computerAimTargetPos + new Vector3(errorRatio * maxErrorFromTarget, 0, errorRatioB * maxErrorFromTarget);
        }

        var eDiff = errorComputerAimTargetPos - computerAimTargetPos;
        var timeUntilShot = computerAimTimeSpan - (Time.time - computerAimStartTime);
        var timeUntilShotRatio = timeUntilShot / computerAimTimeSpan;

        var eTargetPos = errorComputerAimTargetPos + (eDiff * timeUntilShotRatio);
        var eAimVector = eTargetPos - aimPos;

        var shooterPos = aimPos - eAimVector;

        var perfectAimVector = computerAimTargetPos - aimPos;
        Debug.DrawLine(computerAimTargetPos, aimPos, Color.green, 60);
        Debug.DrawLine(eTargetPos, aimPos, Color.blue, 60);
        Debug.DrawLine(shooterPos, aimPos, Color.red, 60);

        Debug.Log("Computer aimed!");

        // Shoot block
        SetShooterPosition(aimBlock, shooterPos);

        if (Time.time - computerAimStartTime > computerAimTimeSpan)
        {
            ShootBlock(aimBlock);
            computerAimBlock = null;
            return GameAction.PlayerShotBlock;
        }

        return GameAction.None;
    }

}


public enum GameState
{
    Starting,
    MainMenu,
    LoadingNewGame,
    Editor,
    Playing,
    EndOfGameReport,
    Closing
}

public enum TurnState
{
    Starting,
    Aiming,
    Shooting,
    Ended
}

public class PlayerInfo
{
    public GameObject baseObject;
    public bool isHuman;

    public bool isAlive
    {
        get
        {
            var baseController = baseObject.GetComponent<BaseController>();
            return baseController.baseState == BaseControllerState.Alive;
        }
    }

    public string name;

    public PlayerInfo(GameObject baseObject, bool isHuman, string name)
    {
        this.baseObject = baseObject;
        this.isHuman = isHuman;
        this.name = name;
    }
}

public enum GameAction
{
    None,
    PlayerShotBlock
}