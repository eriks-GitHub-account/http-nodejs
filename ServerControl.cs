using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firesplash.UnityAssets.SocketIO;
using SimpleJSON;
using UnityEngine.UI;
using UnityEngine.AI;

public class ServerControl : MonoBehaviour
{
    GameStatus gameStatus;

    public SocketIOCommunicator sioCom;
    public Text socketIDInfo;
    public GameObject playerBox;
    public List<GameObject> selectedList;
    public GameObject player;
    public bool myTurn;
    public Button endTurnButton;

    public GameObject building;

    // Start is called before the first frame update
    void Start()
    {
        gameStatus = GameObject.Find("GameStatus").GetComponent<GameStatus>();
        sioCom = GetComponent<SocketIOCommunicator>();

        sioCom.Instance.On("connect", (payload) =>
        {
            Debug.Log("Unity connect event");
            Debug.Log("Connected! Socket ID: " + sioCom.Instance.SocketID);
            socketIDInfo.text = "Socket ID: " + sioCom.Instance.SocketID;


            // Ollaan yhdistetty serveriin. Kerro serverille,
            // ett� halutaan instansioida pelaaja
            JSONObject plJSON = new JSONObject();
            plJSON.Add("Name", gameStatus.saveData.name);
            JSONArray plPosition = new JSONArray();
            plPosition.Add(gameStatus.saveData.location.x);
            plPosition.Add(gameStatus.saveData.location.y);
            plPosition.Add(gameStatus.saveData.location.z);

            plJSON.Add("Position", plPosition);
            string plData = plJSON.ToString();

            sioCom.Instance.Emit("INSTANCEPL", plData, false);

        });

        sioCom.Instance.On("disconnect", (payload) =>
        {
            Debug.Log("Disconnected" + payload);
            sioCom.Instance.Emit("disconnected", "moi", true);
        });


        sioCom.Instance.On("BUILD", (buildingInfo) =>
        {
            Debug.Log("Instancing building");
            JSONNode node = JSON.Parse(buildingInfo);
            Vector3 buildingPosition = new Vector3(node["Position"][0], node["Position"][1], node["Position"][2]);
            GameObject buildingInstance = Instantiate(building, buildingPosition, Quaternion.identity);
            Debug.Log("IIDEEE TIETOKANNASTA: "+ node["id"]);
            buildingInstance.gameObject.name = "Building" + node["id"];
            buildingInstance.GetComponent<Building>().id = node["id"];
            buildingInstance.GetComponent<Building>().res1Needed = int.Parse(node["resources"][0]);
            buildingInstance.GetComponent<Building>().res2Needed = int.Parse(node["resources"][1]);


        });

        sioCom.Instance.On("LOADBUILDINGSFROMDATABASE", (buildingInfo) =>
        {
            sioCom.Instance.Emit("LOADBUILDINGSFROMDB", "testidata", true);
        });
        sioCom.Instance.On("LOADBUILDINGSFROMSERVER", (buildingInfo) =>
        {
            sioCom.Instance.Emit("LOADBUILDINGS", "testidata", true);
        });

        sioCom.Instance.On("INSTANCEPLAYER", (playerInfo) =>
        {
            Debug.Log("Instancing player");
            JSONNode node = JSON.Parse(playerInfo);

            Vector3 playerPosition = new Vector3(node["x"], node["y"], node["z"]);
            GameObject playerInstance = Instantiate(playerBox, playerPosition, Quaternion.identity);
            playerInstance.name = "PlayerBox" + node["socketId"];

            playerInstance.GetComponent<UnitControl>().mySocketID = node["socketId"];
            if (sioCom.Instance.SocketID == playerInstance.GetComponent<UnitControl>().mySocketID)
            {
                playerInstance.GetComponent<UnitControl>().GetSelected();
                player = playerInstance;
                selectedList.Add(playerInstance);
                gameStatus.player = playerInstance;
                playerInstance.GetComponent<UnitControl>().plCamera.SetActive(true);
            }
        });



        sioCom.Instance.On("MOVEUNITS", (unitData) =>
        {
            //T��ll� jokainen pelaaja k�sittelee datan, kun se tulee serverilt�.
            JSONNode node = JSONNode.Parse(unitData);
            // N�m� infot tulostuvat kaikkien pelaajien konsoliin
            Debug.Log("Liikuteltava pelaaja on : " + node["Name"]);
            Debug.Log("Sijainti minne liikkuu on: " + node["Position"][0] + " " + node["Position"][1] + " " + node["Position"][2]);

            float xPos = float.Parse(node["Position"][0]);
            float yPos = float.Parse(node["Position"][1]);
            float zPos = float.Parse(node["Position"][2]);

            GameObject.Find(node["Name"]).GetComponent<UnityEngine.AI.NavMeshAgent>().destination = new Vector3(xPos, yPos, zPos);
            GameObject.Find(node["Name"]).GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = false;
        });

        sioCom.Instance.On("ADDEDRES1", (buildingID) =>
        {
            GameObject.Find("Building" + buildingID).GetComponent<Building>().res1Needed--;
        });

        sioCom.Instance.On("ADDEDRES2", (buildingID) =>
        {
            GameObject.Find("Building"+buildingID).GetComponent<Building>().res2Needed--;
        });


        sioCom.Instance.On("DELETEPLAYER", (unitData) =>
        {
            Destroy(GameObject.Find(unitData));
        });




        //sioCom.Instance.On("ENDTURN", (playerInfo) =>
        //{
        //    JSONNode node = JSON.Parse(playerInfo);
        //    Debug.Log("Player: " + node["socketID"] + " turn ends");
        //    myTurn = false;
        //    endTurnButton.gameObject.SetActive(false);
        //    //Ilmoitetaan serverille, ett� vuoro on p��tetty, jonka seurauksena
        //    //serveri antaa vuoron seuraavalle
        //    sioCom.Instance.Emit("TURNENDED", "testData", true);
        //});

        //sioCom.Instance.On("STARTTURN", (playerInfo) =>
        //{
        //    JSONNode node = JSON.Parse(playerInfo);
        //    Debug.Log("Player: " + node["socketId"] + " turn starts");
        //    myTurn = true;
        //    endTurnButton.gameObject.SetActive(true);
        //});

        sioCom.Instance.Connect();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonUp(0))
        {
            RaycastHit hit;
            Ray ray = player.GetComponent<UnitControl>().plCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.collider.gameObject.CompareTag("Ground"))
                {
                    //Kutsutaan yksik�n funktiota, joka liikuttaa uuteen paikkaan.
                    //Annetaan parametriksi klikattu sijainti.
                    foreach (GameObject selected in selectedList)
                    {
                        selected.GetComponent<UnitControl>().MoveToLocation(hit.point);
                        player.GetComponent<UnitControl>().targetNode = null;
                        player.GetComponent<UnitControl>().readyToGather = false;
                    }
                }else if (hit.collider.gameObject.CompareTag("ResourceNode"))
                {
                    Debug.Log("Resource Node selected: " + hit.collider.gameObject.name);
                    player.GetComponent<UnitControl>().MoveToLocation(hit.point);
                    player.GetComponent<UnitControl>().targetNode = hit.collider.gameObject;
                    player.GetComponent<UnitControl>().readyToGather = true;
                    player.GetComponent<UnitControl>().gatherTimer = 0;

                    player.GetComponent<UnitControl>().targetBuilding = null;
                    player.GetComponent<UnitControl>().readyToBuild = false;
                }
                else if (hit.collider.gameObject.CompareTag("Building"))
                {
                    player.GetComponent<UnitControl>().MoveToLocation(hit.point);
                    if (hit.collider.gameObject.GetComponent<Building>().completed == false)
                    {
                        player.GetComponent<UnitControl>().targetBuilding = hit.collider.gameObject;
                        player.GetComponent<UnitControl>().readyToBuild = true;
                        player.GetComponent<UnitControl>().buildingTimer = 0;
                    }

                    player.GetComponent<UnitControl>().targetNode = null;
                    player.GetComponent<UnitControl>().readyToGather = false;
                }
            }
        } // Hiiren vasen loppuu t�h�n
        if (Input.GetMouseButtonUp(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.collider.gameObject.CompareTag("Ground"))
                {

                        AddBuilding(hit.point, 5, 5, 0);
                    
                }
            }
        }

    }

    public void EndTurn()
    {
        sioCom.Instance.Emit("PLAYERENDTURN", "testidataa", true);
    }

    public void AddPlayer()
    {
        JSONObject plJSON = new JSONObject();
        plJSON.Add("Name", gameStatus.saveData.name);
        // Vector3 sijainti varten
        JSONArray plPosition = new JSONArray();
        plPosition.Add(1.ToString());
        plPosition.Add(2.ToString());
        plPosition.Add(3.ToString());

        plJSON.Add("Position", plPosition);

        string plData = plJSON.ToString();

        sioCom.Instance.Emit("ADDPLAYER", plData, false);
    }

    public void AddBuilding(Vector3 position, int res1, int res2, int buildingStage)
    {
        int id = Random.Range(0, 99999);
        JSONObject bJSON = new JSONObject();
        bJSON.Add("typeID", id.ToString());
        // Vector3 sijainti varten
        JSONArray Position = new JSONArray();
        Position.Add(position.x);
        Position.Add(position.y);
        Position.Add(position.z);
        bJSON.Add("Position", Position);

        JSONArray resources = new JSONArray();
        resources.Add(res1);
        resources.Add(res2);
        bJSON.Add("resources", resources);

        bJSON.Add("buildingStage", buildingStage.ToString());

        string plData = bJSON.ToString();

        sioCom.Instance.Emit("NEWBUILDING", plData, false);
    }
}
