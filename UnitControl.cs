using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitControl : MonoBehaviour
{
    GUIStyle style = new GUIStyle();
    public GameObject plCamera;

    public string mySocketID;
    public bool selected;
    public bool positionSent;

    public bool readyToGather;
    public GameObject targetNode;
    public bool gathering;
    public float gatherTimer;

    public GameObject targetBuilding;
    public bool readyToBuild;
    public bool building;
    public float buildingTimer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (targetNode != null)
        {
            if (gathering && readyToGather)
            {
                Debug.Log("Gathering");
                gatherTimer += Time.deltaTime;
                if (gatherTimer > 3)
                {
                    if (targetNode.GetComponent<ResourceNode>().GatherResource() == "res1")
                    {
                        GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res1++;
                        targetNode.GetComponent<ResourceNode>().resourceAmount--;
                    }
                    else
                    {
                        GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res2++;
                        targetNode.GetComponent<ResourceNode>().resourceAmount--;
                    }
                    gatherTimer = 0;
                }
            }
        }
        else
        {
            gathering = false;
            readyToGather = false;
            gatherTimer = 0;
        }

        if(targetBuilding != null)
        {
            if (readyToBuild && building)
            {
                if (targetBuilding.GetComponent<Building>().completed == true)
                    building = false;
                buildingTimer += Time.deltaTime;
                if(buildingTimer > 3)
                {
                    if(GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res1 > 0 && targetBuilding.GetComponent<Building>().res1Needed > 0)
                    {
                        GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res1--;
                        targetBuilding.GetComponent<Building>().AddRes1();
                    }
                    if (GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res2 > 0 && targetBuilding.GetComponent<Building>().res2Needed > 0)
                    {
                        GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res2--;
                        targetBuilding.GetComponent<Building>().AddRes2();
                    }
                    buildingTimer = 0;
                    if (GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res1 == 0 && GameObject.Find("GameStatus").GetComponent<GameStatus>().saveData.res2 == 0)
                        building = false;
                }
            }
        }
        
    }

    private void OnGUI()
    {
        style.fontSize = 40;
        style.normal.textColor = Color.white;
        if(gathering)
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 100, 100), "GATHERING", style);
        if(building)
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 100, 100), "BUILDING", style);

    }

    public void GetSelected()
    {
        selected = true;
        GetComponent<Renderer>().material.color = Color.red;
    }

    public void GetUnSelected()
    {
        selected = false;
        GetComponent<Renderer>().material.color = Color.green;
    }

    public void MoveToLocation(Vector3 newLocation)
    {
        // Tämä lähettää paikallisen yksikön uuteen sijaintiin Antin omassa pelissä
        GetComponent<NavMeshAgent>().destination = newLocation;
        GetComponent<NavMeshAgent>().isStopped = false;
        positionSent = false;

        // Meidän täytyy kertoa serverille, että Antti on liikuttanut yksikköään. 
        // Serveri lähettää muille pelaajille informaation, mikä yksikkö liikkui mihinkin sijaintiin
        // Serveri tarvitsee tiedot: 1. Kuka liikkui ja 2. Mikä on Vector3 sijainti. 

        JSONObject plJSON = new JSONObject();
        plJSON.Add("Name", gameObject.name);
        // Vector3 sijainti varten
        JSONArray plPosition = new JSONArray();
        plPosition.Add(newLocation.x.ToString());
        plPosition.Add(0.ToString());
        plPosition.Add(newLocation.z.ToString());

        plJSON.Add("Position", plPosition);

        string plData = plJSON.ToString();

        // Käynnistetään serverin pverin päässä MOVE eventti, joka hoitaa liikutuksen muille
        //peli-instansseille.

        GameObject.Find("ServerControl").GetComponent<ServerControl>().sioCom.Instance.Emit("MOVE", plData, false);

    }
}
