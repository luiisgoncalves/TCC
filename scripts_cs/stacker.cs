//// DIGITAL OUTPUTS ////
const string DO_Frente = "Frente";
const string DO_Tras = "Tras";
const string DO_Sobe = "Sobe";
const string DO_Desce = "Desce";
const string DO_Estender = "Estender";
const string DO_Retrair = "Retrair";
//// DIGITAL INPUTS ////
const string DI_LimSwitch0 = "LimSwitch0";
const string DI_LimSwitch1 = "LimSwitch1";
const string DI_LimSwitch2 = "LimSwitch2";
const string DI_LimSwitch3 = "LimSwitch3";
const string DI_Enconder0 = "Enconder0";
const string DI_Enconder1 = "Enconder1";
const string DI_Enconder2 = "Enconder2";
const string DI_Estendido = "Estendido";
const string DI_Recuado = "Recuado";

const float homeMasterRod = 4f;

const float retracted = 0.5f;
const float extended = 1.2f;

const float frente = -0.01f;
const float tras = 0.01f;
const float sobe = 0.01f;
const float desce = -0.01f;

const float speedZ = 0.2f;
const float speedY = 0.2f;
const float speedPiston = 0.5f;

const string X = "X";
const string Y = "Y";
const string Z = "Z";

bool inHome;
bool condHook;
string msg;

DateTime timeLS00 = DateTime.MinValue;
DateTime timeLS01 = DateTime.MinValue;
DateTime timeLS02 = DateTime.MinValue;
DateTime timeLS03 = DateTime.MinValue;
bool condSw00;
bool condSw01;
bool condSw02;
bool condSw03;
int cont = 0;

/////////////////////////////////////////////////////////////////////////////////////////////////////

public void Init()
{
	
}

public void Main()
{
	///////////////////////////////////////////////////////////////////////////////////////////////
	
	UC.SetInput(DI_LimSwitch0, LimitSwitch00.Status);
	UC.SetInput(DI_LimSwitch1, LimitSwitch01.Status);
	UC.SetInput(DI_LimSwitch2, LimitSwitch02.Status);
	UC.SetInput(DI_LimSwitch3, LimitSwitch03.Status);
	
	UC.SetInput(DI_Enconder0, Photocell00.Status);
	UC.SetInput(DI_Enconder1, Photocell01.Status);
	UC.SetInput(DI_Enconder2, Photocell02.Status);
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	
	LimitSwitchStress(UC.GetOutput(DO_Tras),ref condSw00,ref timeLS00,LimitSwitch00,SmokeFX00);
	LimitSwitchStress(UC.GetOutput(DO_Frente),ref condSw01,ref timeLS01,LimitSwitch01,SmokeFX01);
	LimitSwitchStress(UC.GetOutput(DO_Desce),ref condSw02,ref timeLS02,LimitSwitch02,SmokeFX02);
	LimitSwitchStress(UC.GetOutput(DO_Sobe),ref condSw03,ref timeLS03,LimitSwitch03,SmokeFX03);
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	
	inHome = MasterRod.CurrPos()[2] > homeMasterRod && Photocell00.Status;
	condHook = !Piston.IsMoving || Photocell02.Status;
	
	EditorUtils.ShowText(condHook.ToString());
	if(Piston.CurrPos()[2] == retracted)
	{
		UC.SetInput(DI_Recuado, true);
		UC.SetInput(DI_Estendido, false);
	}
	else if(Piston.CurrPos()[2] == extended)
	{
		UC.SetInput(DI_Recuado, false);
		UC.SetInput(DI_Estendido, true);
	}
	else
	{
		UC.SetInput(DI_Recuado, false);
		UC.SetInput(DI_Estendido, false);
	}
	
	if(condHook)
	{
		Hook.Pick(true);
	}
	else
	{
		Hook.Pick(false);
	}
	
	if(UC.GetOutput(DO_Tras) && !LimitSwitch00.Status && !UC.GetOutput(DO_Frente))
	{
		MoveZ(tras);
	} 
	
	if(UC.GetOutput(DO_Frente) && !LimitSwitch01.Status && !UC.GetOutput(DO_Tras))
	{
		MoveZ(frente);
	}
	
	if(UC.GetOutput(DO_Desce) && !LimitSwitch02.Status && !UC.GetOutput(DO_Sobe))
	{
		MoveY(desce);
	}
	
	if(UC.GetOutput(DO_Sobe) && !LimitSwitch03.Status && !UC.GetOutput(DO_Desce))
	{
		MoveY(sobe);
	}
	
	if(UC.GetOutput(DO_Estender) && !UC.GetOutput(DO_Retrair))
	{
		Piston.AnimationMove(Z,speedPiston,extended);
	}
	
	if(UC.GetOutput(DO_Retrair) && !UC.GetOutput(DO_Estender))
	{
		Piston.AnimationMove(Z,speedPiston,retracted);
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	
	if(Input.GetKeyDown(KeyCode.K))
	{
		UC.SetOutput(DO_Frente, true);
	}
	
	if(Input.GetKeyUp(KeyCode.K))
	{
		UC.SetOutput(DO_Frente, false);
	}
	
	if(Input.GetKeyDown(KeyCode.L))
	{
		UC.SetOutput(DO_Tras, true);
	}
	
	if(Input.GetKeyUp(KeyCode.L))
	{
		UC.SetOutput(DO_Tras, false);
	}
	
	if(Input.GetKeyDown(KeyCode.O))
	{
		UC.SetOutput(DO_Sobe, true);
	}
	
	if(Input.GetKeyUp(KeyCode.O))
	{
		UC.SetOutput(DO_Sobe, false);
	}
	
	if(Input.GetKeyDown(KeyCode.I))
	{
		UC.SetOutput(DO_Desce, true);
	}
	
	if(Input.GetKeyUp(KeyCode.I))
	{
		UC.SetOutput(DO_Desce, false);
	}
	
	if(Input.GetKeyDown(KeyCode.E))
	{
		UC.SetOutput(DO_Estender, true);
	}
	
	if(Input.GetKeyUp(KeyCode.E))
	{
		UC.SetOutput(DO_Estender, false);
	}
	
	if(Input.GetKeyDown(KeyCode.R))
	{
		UC.SetOutput(DO_Retrair, true);
	}
	
	if(Input.GetKeyUp(KeyCode.R))
	{
		UC.SetOutput(DO_Retrair, false);
	}
}

public void Physics()
{

}

public void Finish()
{
 
}

public void MoveZ(float direc)
{
	float posZ;
	float newPosZ;
	
	posZ = MasterRod.CurrPos()[2];
	newPosZ = posZ + direc;
	MasterRod.AnimationMove(Z,speedZ,newPosZ);
}

public void MoveY(float direc)
{
	float posY;
	float newPosY;
	
	posY = Cylinder.CurrPos()[1];
	newPosY = posY + direc;
	Cylinder.AnimationMove(Y,speedY,newPosY);
}

public void LimitSwitchStress(bool output,ref bool cond,ref DateTime condition,MechanicSw SW,SmokeFX FX)
{
	if(SW.Status)
	{
		if(output && (DateTime.Now - condition).TotalSeconds >= 1)
		{
			if(cond)
			{
				FX.Visible = true;
			}
			cond = false;
		}
		else
		{
			FX.Visible = false;
			cond = true;
		}
	}
	else
	{
		condition = DateTime.Now;
	}
	
}