using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class CommunicationAcademy : Academy {

    public float agentRunSpeed; 
    public float agentRotationSpeed;
    public Material goalScoredMaterial;
    public Material failMaterial;
    public Material speechMaterial;
    public Material idleMaterial;
    public float gravityMultiplier; 

    public override void InitializeAcademy() {
        Physics.gravity *= gravityMultiplier;
    }

    public override void AcademyReset() {
    }
}
