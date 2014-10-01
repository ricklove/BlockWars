using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class EditorController : MonoBehaviour
{
    public GUISkin mainSkin;

    private bool isAdmin = true;
    private bool shouldShowBossBases = true;

    private BaseStateGroup bossGroup;

    private bool showAdminTextArea;
    private string adminTextAreaText;
    private Vector2 adminTextAreaScrollPosition;

    public EditorState editorState { get; private set; }

    private MouseHelper mouseHelper;
    private GameObject baseObject;

    private float maxDistanceFromBase;

    private Camera gameCamera;
    private Vector3 gameCameraOffset;

    private GameObject editorCrane;

    private GUITexture guiBlockBackground;
    private Vector2 lastScreenSize;

    private GameObject blockAttachedToCrane;
    private float craneDistance = 1.0f;
    private float editorHeight = 0.0f;
    private float editorHeightStepSize = 0.4001f;
    private float editorHeightStepValue = 0.0f;

    private float editorHeightMax = 12.01f;


    private float blockSpaceScreenSize;

    private bool showBlockButtonsGUI = false;

    private int? loadedBaseIndex = null;

    private GameObject[] blockPrefabs;
    private Texture2D[] blockImages;

    public void ShowEditor(GameObject baseObject, float maxDistanceFromBase, GameObject[] blockPrefabs, Texture2D[] blockImages)
    {
        this.baseObject = baseObject;
        this.maxDistanceFromBase = maxDistanceFromBase;
        this.blockPrefabs = blockPrefabs;
        this.blockImages = blockImages;

        editorState = EditorState.Starting;
    }

    protected void OnEditFinished()
    {
        editorState = EditorState.Finished;
    }

    public void FinishedIsHandled()
    {
        editorState = EditorState.NotStarted;
    }

    void Start()
    {
        mouseHelper = GetComponent<MouseHelper>();

        gameCamera = GameObject.Find("GameCamera").camera;
        gameCameraOffset = gameCamera.transform.position;

        editorCrane = GameObject.Find("EditorCrane");

        // TODO: Change this
        var backgroundObj = GameObject.Find("GUIEditorBlockBackground");
        guiBlockBackground = backgroundObj.GetComponent<GUITexture>();
        backgroundObj.transform.position = new Vector3(-1, -1, 0);

    }

    private GUIStyle backStyle;
    private bool showLoadList = false;
    private string[] baseNames;
    private Vector2 loadScrollPosition;

    private bool isOverBlockButton;
    private int? blockToBuildIndex;
    private int? handeledBlockToBuildIndex;
    private float buttonClickedTime;

    void OnGUI()
    {
        GUI.skin = mainSkin;
        // TODO: Create a single OnGUI caller class that will make all OnGUI calls

        if (editorState == EditorState.Starting)
        {
            DisplayBaseEditor(baseObject);
            editorState = EditorState.Active;
        }

        if (editorState == EditorState.Active)
        {
            // Draw the GUI
            var border = 10;
            var buttonHeight = 20;
            var buttonWidth = 70;
            var smallButtonSize = 30;
            var fontWidth = 8;
            var fontHeight = 16;
            var fontPadding = 8;

            GUI.skin.button.fontSize = fontHeight;
            GUI.skin.label.fontSize = fontHeight;


            var sw = Screen.width;
            var sh = Screen.height;

            var blockButtonSpaceSize = Mathf.Min(80, 1.0f * sh / blockImages.Length);

            // Draw the Block Buttons
            if (showBlockButtonsGUI)
            {
                int? bIndex = null;

                int i = 0;
                foreach (var bImage in blockImages)
                {
                    var rect = new Rect(
                      sw - blockButtonSpaceSize - border,
                      (i * blockButtonSpaceSize) + border,
                      blockButtonSpaceSize - border,
                      blockButtonSpaceSize - border);

                    if (GUI.RepeatButton(rect, bImage))
                    {
                        bIndex = i;
                        buttonClickedTime = Time.time;
                        if (handeledBlockToBuildIndex == null)
                        {
                            blockToBuildIndex = i;
                            Debug.Log("Button clicked" + Time.time);
                        }
                    }

                    i++;
                }

                if (bIndex == null)
                {
                    if (Time.time - buttonClickedTime > 0.5f)
                    {
                        handeledBlockToBuildIndex = null;
                        //Debug.Log("No Button clicked" + Time.time);
                    }
                }
            }



            // Draw Height Controls
            float areaWidth = (border * 4.0f) + (buttonWidth * 1.0f);
            float areaHeight = (border * 2.0f) + (smallButtonSize * 1.0f) + (fontHeight * 1.0f) + (fontPadding * 2.0f);

            float areaRight = sw - 2 * border - blockButtonSpaceSize;
            float areaTop = border;
            float areaLeft = areaRight - areaWidth;
            float areaBottom = areaTop + areaHeight;

            //if (backStyle == null)
            //{
            //  backStyle = new GUIStyle();
            //  var state = new GUIStyleState();
            //  state.background = (Texture2D)guiBlockBackground.texture;
            //  backStyle.normal = state;
            //}

            GUI.Box(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "");//, backStyle);
            //var screenRelPos = new Vector3(areaLeft / sw, areaTop / sh, 0);
            //var background = (GUITexture)Instantiate(guiBlockBackground, screenRelPos, guiBlockBackground.transform.rotation);
            //SetLayer(background.gameObject, editorBackgroundLayerNumber);
            //background.gameObject.tag = "Editor";
            //background.pixelInset = new Rect(-areaWidth / 2.0f, -areaHeight / 2.0f, areaWidth, areaHeight);

            // Draw current height step
            var heightStep = (int)((editorHeight / editorHeightStepSize) + 1);

            string heightText = "Height: " + heightStep;
            GUI.Label(new Rect(areaLeft + (border * 1),
              areaTop + (border * 1) + (fontPadding * 1),
              fontWidth * heightText.Length + fontPadding * 2,
              fontHeight + fontPadding * 2), heightText);

            // Draw the Up / Down buttons
            editorHeightStepValue = GUI.HorizontalSlider(
              new Rect(
                areaLeft + (border * 1),
                areaBottom - (border * 3) - (smallButtonSize * 0.0f),
                areaRight - areaLeft - (border * 2),
                border * 2), editorHeightStepValue, 0, 20);

            if (((int)editorHeightStepValue) != heightStep)
            {
                ChangeHeight((int)(editorHeightStepValue - heightStep));
            }

            //if (GUI.Button(new Rect(
            //  areaLeft + (border * 1),
            //  areaBottom - (border * 1) - (smallButtonSize * 1),
            //  smallButtonSize, smallButtonSize), "+"))
            //{
            //  ChangeHeight(1);
            //}

            //if (GUI.Button(new Rect(
            //  areaRight - (border * 1) - (smallButtonSize * 1),
            //  areaBottom - (border * 1) - (smallButtonSize * 1),
            //  smallButtonSize, smallButtonSize), "-"))
            //{
            //  ChangeHeight(-1);
            //}

            // Aligners on/off
            areaWidth = (border * 4.0f) + (buttonWidth * 1.0f);
            areaHeight = (border * 2.0f) + (buttonHeight * 1.0f);

            //areaRight = areaRight;
            areaTop = areaBottom + border;
            areaLeft = areaRight - areaWidth;
            areaBottom = areaTop + areaHeight;

            if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Snap " + (!SimpleJoint.globalDisable ? "On" : "Off")))
            {
                SimpleJoint.globalDisable = !SimpleJoint.globalDisable;
            }

            // Save / Load buttons
            areaTop = areaBottom + border;
            areaBottom = areaTop + areaHeight;

            var fileController = GetComponent<FileController>();

            if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Save"))
            {
                var baseState = fileController.GetGameBaseState(baseObject, maxDistanceFromBase);

                string[] names = fileController.GetBaseStateNames();

                int i = 0;
                var prefix = "B";
                var tempName = prefix + i;
                while (Array.IndexOf(names, tempName) >= 0)
                {
                    i++;
                    tempName = prefix + i;
                }

                fileController.SaveBaseState(tempName, baseState);
            }

            areaTop = areaBottom + border;
            areaBottom = areaTop + areaHeight;

            if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Load"))
            {
                showLoadList = true;

                var bNames = new List<string>();
                bNames.AddRange(fileController.GetBaseStateNames());

                if (shouldShowBossBases)
                {
                    bossGroup = fileController.LoadBaseGroupFile("BaseData/BossBases");
                    bNames.AddRange(bossGroup.baseNames);
                }

                baseNames = bNames.ToArray();
            }

            areaTop = areaBottom + border;
            areaBottom = sh - border;

            if (showLoadList)
            {
                var namesHeight = baseNames.Length * buttonHeight;

                var areaRect = new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop);
                var gridRect = new Rect(0, 0, areaRight - areaLeft - 2 * border, namesHeight);
                int selectedIndex;

                if (areaRect.height > gridRect.height)
                {
                    selectedIndex = GUI.SelectionGrid(areaRect, -1, baseNames, 1);
                }
                else
                {
                    loadScrollPosition = GUI.BeginScrollView(areaRect, loadScrollPosition, gridRect);
                    selectedIndex = GUI.SelectionGrid(gridRect, -1, baseNames, 1);
                    GUI.EndScrollView();
                }

                if (selectedIndex >= 0)
                {
                    var baseName = baseNames[selectedIndex];
                    BaseState baseState = null;

                    if (baseName.StartsWith("Boss"))
                    {
                        var bi = Array.IndexOf(bossGroup.baseNames, baseName);
                        baseState = bossGroup.baseStates[bi];
                    }
                    else
                    {
                        baseState = fileController.LoadBaseState(baseNames[selectedIndex]);
                    }

                    fileController.SetGameBaseState(blockPrefabs, baseObject, baseState, Consts.editorLayerNumber, maxDistanceFromBase);

                    loadedBaseIndex = selectedIndex;

                    showLoadList = false;

                    // Balance loaded blocks
                    foreach (Transform block in baseObject.transform)
                    {
                        BalancePlacedBlock(block.gameObject);
                    }
                }

                //foreach (var name in baseNames)
                //{
                //  if (GUI.Button())
                //  {
                //    var baseState = fileController.LoadBaseState(name);
                //    fileController.SetGameBaseState("Base", baseState);
                //    showLoadList = false;
                //  }
                //}


            }

            areaRight = areaLeft - border;
            areaTop = border;
            areaLeft = areaRight - areaWidth;
            areaBottom = areaTop + areaHeight;

            // Only show this if a block is touching the base
            if (baseObject.GetComponent<BaseController>().isTouchingBlock)
            {
                if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Done"))
                {
                    OnEditFinished();
                }
            }

            if (isAdmin)
            {
                areaTop = areaBottom + border;
                areaBottom = areaTop + areaHeight;

                if (loadedBaseIndex.HasValue)
                {
                    if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Delete"))
                    {
                        fileController.DeleteBaseState(baseNames[loadedBaseIndex.Value]);
                        loadedBaseIndex = null;
                    }
                }

#if UNITY_STANDALONE_WIN
        areaTop = areaBottom + border;
        areaBottom = areaTop + areaHeight;

        if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Export"))
        {
          showAdminTextArea = !showAdminTextArea;
          if (showAdminTextArea)
          {
            var exportData = fileController.ExportBases();
            var tempFileName = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFileName, exportData);
            adminTextAreaText = tempFileName;
          }
        }

        if (showAdminTextArea)
        {
          areaTop = areaBottom + border;
          areaBottom = areaTop + areaHeight;

          if (GUI.Button(new Rect(areaLeft, areaTop, areaRight - areaLeft, areaBottom - areaTop), "Import"))
          {
            //showAdminTextArea = !showAdminTextArea;

            try
            {
              var importData = System.IO.File.ReadAllText(adminTextAreaText);
              fileController.ImportBases(importData);
              Debug.Log("Import Succeeded");
            }
            catch (Exception ex)
            {
              Debug.Log("FAIL: Import Failed: " + ex.ToString());
            }
          }
        }

        if (showAdminTextArea)
        {
          var scrollW = areaLeft - 2 * border;
          var scrollH = sh - 2 * border;
          //adminTextAreaScrollPosition = GUI.BeginScrollView(
          //  new Rect(border, border, scrollW + 4 * border, scrollH + 4 * border),
          //  adminTextAreaScrollPosition,
          //  new Rect(0, 0, scrollW, scrollH), false, true);

          adminTextAreaText = GUI.TextArea(new Rect(border, border, scrollW - 2 * border, scrollH - 2 * border), adminTextAreaText);
          //GUI.EndScrollView();
        }

#endif
            }
        }
    }

    void ChangeHeight(int stepChange)
    {
        editorHeight += (editorHeightStepSize * stepChange);

        if (editorHeight < 0)
        {
            editorHeight = 0;
        }

        if (editorHeight > editorHeightMax)
        {
            editorHeight = editorHeightMax;
        }
    }

    void Update()
    {
        // BUG: One time when I clicked the new editor block, it used a base block that was already in place
        // TODO: Switch to base layer (when done editing) and back to edit layer (when editing)

        if (editorState == EditorState.Active)
        {
            BalancePlacedBlocks();

            // If screen size changes, then refresh layout
            var screenSize = new Vector2(Screen.width, Screen.height);

            if (lastScreenSize != screenSize)
            {
                LayoutEditor();
                lastScreenSize = screenSize;
            }

            // Mouse down
            if (blockToBuildIndex != null || Input.GetButtonDown("Fire1"))
            {
                // If clicked on the preview window, then change cameras (while mouse is held)
                // Otherwise edit

                GameObject hitObj = null;
                var blockPos = new Vector3(0, 0, 0);

                var centerScreenPoint = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 5));
                var centerScreenPointB = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f + 5, 5));

                if (blockToBuildIndex != null)
                {
                    var mPosWorld = GetMouseEditorPosition();

                    hitObj = blockPrefabs[blockToBuildIndex.Value];

                    // Attach the crane to a new block at the current height
                    blockPos = mPosWorld;

                    handeledBlockToBuildIndex = blockToBuildIndex;
                    blockToBuildIndex = null;

                    Debug.DrawLine(centerScreenPointB, blockPos, Color.yellow, 3);
                }
                else
                {
                    var mPos = Input.mousePosition;
                    Ray ray = gameCamera.ScreenPointToRay(mPos);
                    RaycastHit hit;

                    // Anything but the editorHeightPlane
                    var layerMask = 1 << Consts.mouseHeightPlaneLayerNumber;
                    var layerMask2 = 1 << Consts.ignoreRaycastLayerNumber;
                    layerMask = layerMask | layerMask2;

                    layerMask = ~layerMask;

                    Debug.DrawLine(centerScreenPointB, GetMouseEditorPosition(), Color.red, 3);

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
                    {
                        //Debug.DrawLine(ray.origin, hit.point);
                        Debug.DrawLine(centerScreenPoint, hit.point, Color.yellow, 3);
                        Debug.DrawLine(ray.origin, hit.point, Color.red, 3);

                        hitObj = hit.collider.gameObject;

                        // Get Block Object (which is a parent of the block mesh or the aligners)
                        hitObj = hitObj.transform.parent.gameObject;

                        // Attach the crane to a new block at the current height
                        blockPos = new Vector3(hit.point.x, editorHeight, hit.point.z);

                        // DEBUG
                        if (hitObj.tag == "EditorBlock" || hitObj.tag == "Block")
                        {
                            Debug.DrawLine(ray.origin, hit.point, Color.green, 3);
                        }
                    }
                }

                if (hitObj != null)
                {
                    if (hitObj.tag == "EditorBlock" || hitObj.tag == "Block")
                    {
                        Debug.DrawLine(centerScreenPoint, blockPos, Color.blue, 3);

                        // Move crane to mouse
                        editorCrane.transform.position = GetCranePosition();

                        if (hitObj.tag == "EditorBlock")
                        {
                            blockAttachedToCrane = (GameObject)Instantiate(hitObj, blockPos, hitObj.transform.rotation) as GameObject;

                            blockAttachedToCrane.AddComponent<MirrorObject>();
                        }
                        else
                        {
                            blockAttachedToCrane = hitObj;
                            blockAttachedToCrane.transform.position = blockPos;
                            blockAttachedToCrane.transform.eulerAngles = new Vector3(
                              blockPrefabs[0].transform.eulerAngles.x,
                              blockAttachedToCrane.transform.eulerAngles.y,
                              blockPrefabs[0].transform.eulerAngles.z);
                        }

                        var blockRigidbody = blockAttachedToCrane.GetComponent<Rigidbody>();
                        blockRigidbody.isKinematic = false;
                        blockRigidbody.drag = 1.5f;

                        // Attach spring
                        var sJoint = editorCrane.GetComponent<SpringJoint>();
                        sJoint.connectedBody = blockRigidbody;


                        sJoint.anchor = new Vector3();
                        sJoint.connectedAnchor = new Vector3();

                        // Floating the anchor helps
                        var baseAnchor = new Vector3(0, -1f, 0);
                        var offsetAnchor = new Vector3(0, 0, 0.5f);
                        var offsetConnected = new Vector3(0, 0, 0);
                        //var offsetConnected = new Vector3(-0.1f, 0, 0.5f);

                        sJoint.anchor = new Vector3(baseAnchor.x + offsetAnchor.x, baseAnchor.y + offsetAnchor.y, baseAnchor.z + offsetAnchor.z); ;
                        sJoint.connectedAnchor = new Vector3(baseAnchor.x + offsetConnected.x, baseAnchor.y + offsetConnected.y, baseAnchor.z + offsetConnected.z); ;


                        blockRigidbody.constraints =
                          RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ |
                          RigidbodyConstraints.FreezePositionY;


                        // Remove from blocks to place
                        var bb = _blocksToBalance.Where(b => blockAttachedToCrane == b.Block).FirstOrDefault();
                        if (bb != null)
                        {
                            _blocksToBalance.Remove(bb);
                        }
                    }

                    //Gizmos.color = Color.blue;
                    //Gizmos.DrawLine(centerScreenPoint, hit.point);

                    //var clone = (GameObject)Instantiate(blockSquarePrefab, transform.position, transform.rotation) as GameObject;
                }
            }

            // Mouse dragging
            if (Input.GetButton("Fire1"))
            {
                if (blockAttachedToCrane != null)
                {
                    editorCrane.transform.position = GetCranePosition();

                    if ((blockAttachedToCrane.transform.position - baseObject.transform.position).magnitude > maxDistanceFromBase)
                    {
                        // TODO: If block is outside base, then show a warning it will not be used
                        Debug.DrawLine(blockAttachedToCrane.transform.position, baseObject.transform.position, Color.red, 3.0f);
                    }
                    else
                    {
                        Debug.DrawLine(blockAttachedToCrane.transform.position, baseObject.transform.position, Color.green, 3.0f);
                    }
                }
            }

            if (Input.GetButtonUp("Fire1"))
            {
                if (blockAttachedToCrane != null)
                {
                    // Detach block
                    var sJoint = editorCrane.GetComponent<SpringJoint>();
                    sJoint.connectedBody = null;

                    // If block is outside base, destroy it
                    if ((blockAttachedToCrane.transform.position - baseObject.transform.position).magnitude > maxDistanceFromBase)
                    {
                        Destroy(blockAttachedToCrane);
                        blockAttachedToCrane = null;
                    }
                    else
                    {
                        var blockRigidbody = blockAttachedToCrane.GetComponent<Rigidbody>();
                        blockRigidbody.isKinematic = false;
                        blockRigidbody.drag = 1;

                        // Stop the block from moving
                        blockRigidbody.velocity = Vector3.zero;
                        blockRigidbody.angularVelocity = Vector3.zero;

                        // Freeze block
                        blockRigidbody.constraints = RigidbodyConstraints.None;
                        BalancePlacedBlock(blockAttachedToCrane);


                        blockAttachedToCrane.transform.parent = baseObject.transform;
                        blockAttachedToCrane.tag = "Block";
                        blockAttachedToCrane = null;
                    }

                }
            }
        }
    }

    private List<BlockBalance> _blocksToBalance = new List<BlockBalance>();

    private void BalancePlacedBlock(GameObject block)
    {
        _blocksToBalance.Add(new BlockBalance(block));
    }

    private bool _soYouCanDance = false;

    public class BlockBalance
    {
        public GameObject Block { get; set; }

        public bool isFrozen { get; set; }
        public Vector3 lastPosition { get; set; }

        public BlockBalance(GameObject block)
        {
            Block = block;
        }
    }

    private void BalancePlacedBlocks()
    {
        var blocksNotDone = new List<BlockBalance>();

        foreach (var blockBalance in _blocksToBalance)
        {
            var block = blockBalance.Block;

            if (block == null)
            {
                continue;
            }

            if (!block.activeSelf)
            {
                continue;
            }

            if (blockBalance.isFrozen)
            {
                continue;
            }

            // BREAKDANCE BUG
            if (_soYouCanDance)
            {
                var rotationMod = 180.0f / 6.0f;

                var lRotation = block.transform.localEulerAngles;

                var ex = Mathf.Round((int)(lRotation.x / rotationMod)) * rotationMod;
                var ey = Mathf.Round((int)(lRotation.y / rotationMod)) * rotationMod;
                var ez = Mathf.Round((int)(lRotation.z / rotationMod)) * rotationMod;

                block.transform.localEulerAngles = new Vector3(ex, ey, ez);
            }
            else
            {
                // Rotate to an even rotation %15 degrees
                var rotationMod = 180.0f / 12.0f;

                var lRotation = block.transform.localEulerAngles;

                var ex = Mathf.Round(lRotation.x / rotationMod) * rotationMod;
                var ey = Mathf.Round(lRotation.y / rotationMod) * rotationMod;
                var ez = Mathf.Round(lRotation.z / rotationMod) * rotationMod;

                //// Only allow flat rotation
                //ex = 0;
                //ez = 0;

                block.transform.localEulerAngles = new Vector3(ex, ey, ez);
            }

            var pos = block.transform.localPosition;
            var change = Vector3.Distance(pos, blockBalance.lastPosition);
            blockBalance.lastPosition = pos;

            if (change > 0 && change < 0.001)
            {
                // Freeze when balanced
                var blockRigidbody = block.GetComponent<Rigidbody>();
                blockRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                blockBalance.isFrozen = true;
            }
            else
            {
                blocksNotDone.Add(blockBalance);
            }
        }

        _blocksToBalance.Clear();
        _blocksToBalance = blocksNotDone;

    }

    private Vector3 GetCranePosition()
    {
        var mPosWorld = GetMouseEditorPosition();
        var cranePoint = new Vector3(mPosWorld.x, mPosWorld.y + craneDistance, mPosWorld.z);

        return cranePoint;
    }

    private Vector3 GetBlockPosition()
    {
        return GetMouseEditorPosition();
    }

    private Vector3 GetMouseEditorPosition()
    {
        return mouseHelper.GetMousePosition(editorHeight);
    }


    void DisplayBaseEditor(GameObject baseMarker)
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        mouseHelper.MoveCameraToLookAtGroundPoint(baseMarker.transform.position.x, baseMarker.transform.position.z);
        showBlockButtonsGUI = true;
        //MoveButtonPlaneToButtons(editorHeight);
        //LayoutEditor();
    }

    //private void MoveCameraToBase(float baseCenterX, float baseCenterZ)
    //{
    //  gameCamera.transform.position = new Vector3(baseCenterX + gameCameraOffset.x, gameCameraOffset.y, baseCenterZ + gameCameraOffset.z);
    //}

    private void LayoutEditor()
    {
        const int paddingPixels = 5;

        // Clean up old editor display
        var editorObjs = GameObject.FindGameObjectsWithTag("Editor");
        foreach (var eObj in editorObjs)
        {
            Destroy(eObj);
        }

        // Put one of each shape on the side
        var sw = Screen.width;
        var sh = Screen.height;

        var blockTypes = blockPrefabs;

        // Block size based on actual world size on screen
        var blockSpaceSizeWithPadding = 2.5f;
        blockSpaceScreenSize = (gameCamera.WorldToScreenPoint(new Vector3(blockSpaceSizeWithPadding, 0, 0)) -
          gameCamera.WorldToScreenPoint(new Vector3(0, 0, 0))).x;

        var totalSize = Mathf.Max(5 * blockSpaceScreenSize, blockTypes.Length * blockSpaceScreenSize);

        var orthoSize = totalSize / 120;

        // Keep adjusting smaller until small enough
        while (totalSize > sw ||
          totalSize > sh)
        {
            orthoSize += 1;

            gameCamera.transform.localPosition = new Vector3(-orthoSize * 0.0f, -orthoSize, -orthoSize);

            // Recalculate the block space size for the screen positioning
            blockSpaceScreenSize = (gameCamera.WorldToScreenPoint(new Vector3(blockSpaceSizeWithPadding, 0, 0)) -
              gameCamera.WorldToScreenPoint(new Vector3(0, 0, 0))).x;

            totalSize = Mathf.Max(5 * blockSpaceScreenSize, blockTypes.Length * blockSpaceScreenSize);
        }

        var backgroundSize = blockSpaceScreenSize * 1.1f;

        int i = 0;
        foreach (var blockType in blockTypes)
        {

            var screenPos = new Vector3(sw - (blockSpaceScreenSize / 2) - paddingPixels, sh - (i * blockSpaceScreenSize) - (blockSpaceScreenSize / 2) - paddingPixels, 0);
            var worldPosOfScreen = gameCamera.ScreenToWorldPoint(screenPos);
            var screenRelPos = new Vector3(screenPos.x / sw, screenPos.y / sh, 0);

            // Put below stage out of the way (so it won't collide with anything)
            var worldPos = new Vector3(worldPosOfScreen.x, -10, worldPosOfScreen.z);


            var background = (GUITexture)Instantiate(guiBlockBackground, screenRelPos, guiBlockBackground.transform.rotation);
            //Consts.SetLayer(background.gameObject, Consts.editorBackgroundLayerNumber);
            background.gameObject.tag = "Editor";
            background.pixelInset = new Rect(-backgroundSize / 2.0f, -backgroundSize / 2.0f, backgroundSize, backgroundSize);


            var clone = (GameObject)Instantiate(blockType, worldPos, blockType.transform.rotation);
            //Consts.SetLayer(clone, Consts.editorLayerNumber);
            clone.gameObject.tag = "EditorBlock";
            clone.GetComponent<Rigidbody>().isKinematic = true;

            i++;
        }

    }





}

public enum EditorState
{
    NotStarted,
    Starting,
    Active,
    Finished
}