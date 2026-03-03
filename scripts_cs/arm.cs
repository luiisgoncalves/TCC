//// DIGITAL OUTPUTS ////
const string DO_Vacuo = "Vacuo";
const string DO_Levanta = "Levanta";
const string DO_Abaixa = "Abaixa";
const string DO_Estende = "Estende";
const string DO_Recua = "Recua";
const string DO_Horario = "Horario";
const string DO_AntiHorar = "AntiHorar";

//// DIGITAL INPUTS ////
const string DI_Vacuo = "Vacuo";
const string DI_InductSens = "InductSens";
const string DI_Estendido = "Estendido";
const string DI_Recuado = "Recuado";
const string DI_Levantado = "Levantado";
const string DI_Abaixado = "Abaixado";
const string DI_encoder = "Encoder";

const int horario = 0;
const int antihor = 1;
const string axisY = "Y";
const string axisZ = "Z";
const float rotBase = 180;
const float speedRotBase = 10f;
const float speedPiston = 0.6f;
const float volumeSoundFX = 1f;
const float horizontalInit = 0.3f;
const float horizontalTarg = 1f;
const float verticalInit = 0.5f;
const float verticalTarg = 1.5f;

bool advanArm = false;
bool raiseArm = false;
bool aux = false;
int cont = 0;
bool autHor = false;
bool autAntHor = false;

public void Init()
{
	SmokeFX.Embers = true;
}

public void Main()
{
	MovePiston(VerticalPiston,DO_Levanta,DO_Abaixa,axisZ,speedPiston,verticalInit,verticalTarg,DI_Levantado,DI_Abaixado);
	MovePiston(HorizontalPiston,DO_Estende,DO_Recua,axisZ,speedPiston,horizontalInit,horizontalTarg,DI_Estendido,DI_Recuado);
	UC.SetInput(DI_InductSens,InductiveSwitch.Status);
	UC.SetInput(DI_encoder,InductiveSwitchENCODER.Status);
	
	if (InductiveSwitchENCODER.Status && UC.GetOutput(DO_Horario) && autHor)
	{
		cont++;
		autHor = false;
	}
	
	if (InductiveSwitchENCODER.Status && UC.GetOutput(DO_AntiHorar) && autAntHor)
	{
		cont--;
		autAntHor = false;
	}
	
	else if (!InductiveSwitchENCODER.Status)
	{
		autHor = true;
		autAntHor = true;
	}
	
	if (UC.GetOutput(DO_Horario) && !UC.GetOutput(DO_AntiHorar))
	{
		Base.AnimationRotate(axisY,rotBase,speedRotBase,horario);
	}
	
	else if (UC.GetOutput(DO_AntiHorar) && !UC.GetOutput(DO_Horario))
	{
		Base.AnimationRotate(axisY,rotBase,speedRotBase,antihor);
	}
	
	else 
	{
		Base.AnimationRotate(axisY,0,speedRotBase,antihor);
	}
	
	if(UC.GetOutput(DO_Horario) && UC.GetOutput(DO_AntiHorar))
	{
		SmokeFX.Visible = true;
		
	}
	else
	{
		SmokeFX.Visible = false;
	}
	
	if (UC.GetOutput(DO_Vacuo))
	{
		Hook.Pick(true);
		UC.SetInput(DI_Vacuo, true);
		EditorUtils.PlayLoopSound("Suction", volumeSoundFX);
	}
	else
	{
		Hook.Pick(false);
		UC.SetInput(DI_Vacuo, false);
		EditorUtils.StopSound();
	}
	
	////////////////////////////////////////////////////////////
	
	if(Input.GetKeyDown(KeyCode.T))
	{
		raiseArm = !raiseArm;
		UC.SetOutput(DO_Levanta, raiseArm);
	}
	
	if(Input.GetKeyDown(KeyCode.Y))
	{
		advanArm = !advanArm;
		UC.SetOutput(DO_Estende, advanArm);
	}
}

public void Physics()
{

}

public void Finish()
{
 
}

public void MovePiston(Model3D piston,
					   string outputExt,
					   string outputRec,
					   string direcAxis,
					   float speed,
					   float posInit,
					   float posTarg,
					   string sensorExt,
					   string sensorRec)
{	
	
	if (UC.GetOutput(outputExt))
	{
		piston.AnimationMove(direcAxis, speed, posTarg);
	}
	else
	{
		piston.AnimationMove(direcAxis, speed, posInit);
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