using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class CommunicationAgent : Agent
{
    public GameObject ground;
    public GameObject area;
    public GameObject orangeGoal;
    public GameObject redGoal;
    public GameObject floatingText;
    public GameObject cameraGO;
    public bool useVectorObs;

    public bool isManager;
    public CommunicationAgent PartnerAgent;

    private List<GameObject> texts = new List<GameObject>();

    RayPerception rayPer;
    Rigidbody shortBlockRB;
    Rigidbody agentRB;
    Material groundMaterial;
    Renderer groundRenderer;
    CommunicationAcademy academy;
    [SerializeField]
    int selection;
    [SerializeField]
    int percievedSelection;
    [SerializeField]
    int percievedSelectionCounter;
    [SerializeField]
    int percievedSelectionCounterMax = 32;
    [SerializeField]
    int enteredGoal = 0;
    [SerializeField]
    int enteredFail = 0;
    bool countPercievedSelection;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        academy = FindObjectOfType<CommunicationAcademy>();
        rayPer = GetComponent<RayPerception>();
        agentRB = GetComponent<Rigidbody>();
        groundRenderer = ground.GetComponent<Renderer>();
        groundMaterial = groundRenderer.material;
    }

    public override void CollectObservations()
    {
        if (useVectorObs)
        {
            float rayDistance = 12f * 4;
            float[] rayAngles = {60f, 90f, 120f};
            string[] detectableObjects = { "orangeGoal", "redGoal", "wall" };
            AddVectorObs(GetStepCount() / (float)agentParameters.maxStep);
            AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));
            AddVectorObs(percievedSelection);
            IncrementPercievedSelectionCounter();
        }
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        groundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        groundRenderer.material = groundMaterial;
    }

    public void SaySelection(CommunicationAgent agent, int saidSelection)
    {
        agent.PerceiveSelection(saidSelection);
    }

    public void SpawnFloatingText()
    {
        if (texts.Count < 4)
        {
            var textGO = Instantiate(floatingText, transform.position, cameraGO.transform.rotation);
            string text = "";
            if (percievedSelection == 1)
            {
                text = "Bottom";
            }
            if (percievedSelection == 2)
            {
                text = "Top";
            }
            textGO.GetComponent<TextMesh>().text = text;
            texts.Add(textGO);
        }
    }

    public void PerceiveSelection(int saidSelection)
    {
        percievedSelection = saidSelection;
        percievedSelectionCounter = 0;
        countPercievedSelection = true;
    }

    public void IncrementPercievedSelectionCounter()
    {
        if (countPercievedSelection)
        {
            percievedSelectionCounter += 1;
            if (percievedSelectionCounter > percievedSelectionCounterMax)
            {
                ResetPercievedSelection(this);
            }
        }
    }

    public void ResetPercievedSelection(CommunicationAgent agent)
    {
        agent.percievedSelection = 0;
        agent.percievedSelectionCounter = 0;
        agent.countPercievedSelection = false;
    }

    public void MoveAgent(float[] act)
    {

        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;

        if (brain.brainParameters.vectorActionSpaceType == SpaceType.continuous)
        {
            dirToGo = transform.forward * Mathf.Clamp(act[0], -1f, 1f);
            rotateDir = transform.up * Mathf.Clamp(act[1], -1f, 1f);
        }
        else
        {
            int action = Mathf.FloorToInt(act[0]);
            switch (action)
            {
                case 1:
                    dirToGo = transform.forward * 1f;
                    break;
                case 2:
                    dirToGo = transform.forward * -1f;
                    break;
                case 3:
                    rotateDir = transform.up * 1f;
                    break;
                case 4:
                    rotateDir = transform.up * -1f;
                    break;
                case 5:
                    if (isManager)
                    {
                        SaySelection(this, selection);
                        SaySelection(PartnerAgent, selection);
                        SpawnFloatingText();
                        // StartCoroutine(GoalScoredSwapGroundMaterial(academy.speechMaterial, 0.1f));
                        // AddReward(1f / agentParameters.maxStep);
                        AddReward(- 1f / agentParameters.maxStep);
                    }
                    break;
            }
        }
        transform.Rotate(rotateDir, Time.deltaTime * 150f);
        agentRB.AddForce(dirToGo * academy.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        AddReward(-2f / agentParameters.maxStep);
        MoveAgent(vectorAction);
    }

    public void AddRewardToEveryone(float reward)
    {
        this.AddReward(reward);
        PartnerAgent.AddReward(reward);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("orangeGoal") || col.gameObject.CompareTag("redGoal"))
        {
            if ((selection == 1 && col.gameObject.CompareTag("orangeGoal")) ||
                (selection == 2 && col.gameObject.CompareTag("redGoal")))
            {
                if (enteredGoal == 0)
                {
                    enteredGoal = 1;
                    AddRewardToEveryone(1f);
                }
                StartCoroutine(GoalScoredSwapGroundMaterial(academy.goalScoredMaterial, 0.5f));
            }
            else
            {
                if (enteredFail == 0)
                {
                    enteredFail = 1;
                    AddRewardToEveryone(-0.25f);
                }
                StartCoroutine(GoalScoredSwapGroundMaterial(academy.failMaterial, 0.5f));
            }
        }
    }

    void OnCollisionStay(Collision col)
    {
        if (col.gameObject.CompareTag("orangeGoal") || col.gameObject.CompareTag("redGoal"))
        {
            if ((selection == 1 && col.gameObject.CompareTag("orangeGoal")) ||
                (selection == 2 && col.gameObject.CompareTag("redGoal")))
            {
                AddRewardToEveryone(3f / agentParameters.maxStep);
            }
        }
    }

    public override void AgentReset()
    {
        if (isManager)
        {
            selection = Random.Range(1, 3);
            PartnerAgent.selection = selection;
            ResetAgent(this, 2f);
            ResetAgent(PartnerAgent, -2f);
        }
    }

    public void ResetAgent(CommunicationAgent agent, float agentOffset)
    {
        ResetPercievedSelection(agent);
        agent.enteredGoal = 0;
        agent.enteredFail = 0;

        if (texts.Count > 0)
        {
            foreach(GameObject go in texts)
            {
                Destroy(go);
            }
            texts.Clear();
        }
 
        agent.transform.position = new Vector3(agentOffset + Random.Range(-1f, 1f),
                                        1f, 0f)
            + ground.transform.position;
        agent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        agent.GetComponent<Rigidbody>().velocity *= 0f;
    }
}
