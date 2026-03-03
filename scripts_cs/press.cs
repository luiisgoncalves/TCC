//// DIGITAL OUTPUTS ////
const string DO_pistaoHorizExt = "pistHorExt";
const string DO_pistaoHorizRec = "pistHorRec";
const string DO_pistaoPrenExt = "pisPrenExt";
const string DO_pistaoPrenRec = "pisPrenRec";
const string DO_pistaoPortExt = "pistPorExt";
const string DO_pistaoPortRec = "pistPorRec";
//// DIGITAL INPUTS ////
const string DI_CestoEst = "CestoEst";
const string DI_CestoRec = "CestoRec";
const string DI_PrensaOn = "PrensaOn";
const string DI_PrensaOff = "PrensaOff";
const string DI_PortaFech = "PortaFech";
const string DI_PortaAbert = "PortaAbert";

float actuatorPosInit = 0.2f;
float actuatorPosTarg = 1.3f;
float actuatorMovSpeed = 1f;
string actuatorDirecAxis = "Z";

float pressPosInit = 0.1f;
float pressPosTarg = 0.5f;
float pressMovSpeed = 2f;
string pressDirecAxis = "Z";

float doorPosInit = -0.11f;
float doorPosTarg = -0.5f;
float doorMovSpeed = 1f;
string doorDirecAxis = "Z";

public void Init()
{
 
}

public void Main()
{
	VerifyColission(UC.GetOutput(DO_pistaoHorizExt),UC.GetOutput(DO_pistaoPrenExt),UC.GetOutput(DO_pistaoPortExt));
	MovePiston(PistonHor,DO_pistaoHorizExt,DO_pistaoHorizRec,actuatorDirecAxis,actuatorMovSpeed,actuatorPosInit,actuatorPosTarg,DI_CestoRec,DI_CestoEst);
	MovePiston(PistonPress,DO_pistaoPrenExt,DO_pistaoPrenRec,pressDirecAxis,pressMovSpeed,pressPosInit,pressPosTarg,DI_PrensaOff,DI_PrensaOn);
	MovePiston(PistonDoor,DO_pistaoPortExt,DO_pistaoPortRec,doorDirecAxis,doorMovSpeed,doorPosInit,doorPosTarg,DI_PortaAbert,DI_PortaFech);
	
	if(!(UC.GetInput(DI_CestoEst) || UC.GetInput(DI_CestoRec)) || 
	   !(UC.GetInput(DI_PrensaOn) || UC.GetInput(DI_PrensaOff)) ||
	   !(UC.GetInput(DI_PortaFech) || UC.GetInput(DI_PortaAbert)))
	{
		EditorUtils.PlayLoopSound("Suction", 1f);
	}
	else
	{
		EditorUtils.StopSound();
	}
		
}

public void Physics()
{

}

public void Finish()
{
 
}

public void VerifyColission(bool piston1, bool piston2, bool piston3)
{
	if(UC.GetInput(DI_CestoEst) && UC.GetInput(DI_PrensaOn) || UC.GetInput(DI_CestoEst) && UC.GetInput(DI_PortaFech))
	{
		ExplosionFX.Visible = true;
	}
	else
	{
		ExplosionFX.Visible = false;
	}
}

public void MovePiston(Model3D piston,
						string pistonExt,
						string pistonRec,
						string direcAxis,
						float movSpeed,
						float posInit,
						float posTarg,
						string sensorRec,
						string sensorExt)
{
	if(UC.GetOutput(pistonExt) && !UC.GetOutput(pistonRec))
	{
		piston.AnimationMove(direcAxis,movSpeed,posTarg);
	}
	
	if(UC.GetOutput(pistonRec) && !UC.GetOutput(pistonExt))
	{
		piston.AnimationMove(direcAxis,movSpeed,posInit);
	}
	
	if(piston.GetAnimationPosition(direcAxis) == posTarg)
	{
		UC.SetInput(sensorExt,true);
	}
	else
	{
		UC.SetInput(sensorExt,false);
	}
	
	if(piston.GetAnimationPosition(direcAxis) == posInit)
	{
		UC.SetInput(sensorRec,true);
	}
	else
	{
		UC.SetInput(sensorRec,false);
	}
}