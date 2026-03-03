//// DIGITAL OUTPUTS ////
const int DO_ConveyorBelt01_Advance = 0;
const int DO_PneumaticPushTop_Advance = 1;
const int DO_PneumaticPushTop_Back = 2;
const int DO_PneumaticPushBottom_Advance = 3;
const int DO_PneumaticPushBottom_Back = 4;
const int DO_PneumaticPush01_Advance = 5;
const int DO_PneumaticPush01_Back = 6;
const int DO_PneumaticPush02_Advance = 7;
const int DO_PneumaticPush02_Back = 8;
const int DO_PneumaticPush04_Advance = 9;
const int DO_PneumaticPush04_Back = 10;
const int DO_Braco_Vacuo = 11;
const int DO_Braco_Levanta = 12;
const int DO_Braco_Abaixa = 13;
const int DO_Braco_Estende = 14;
const int DO_Braco_Recua = 15;
const int DO_Braco_Horario = 16;
const int DO_Braco_AntiHorar = 17;
const int DO_Prensa_pistHorExt = 18;
const int DO_Prensa_pistHorRec = 19;
const int DO_Prensa_pistPortExt = 20;
const int DO_Prensa_pistPortRec = 21;
const int DO_Prensa_pistPrenExt = 22;
const int DO_Prensa_pistPrenRec = 23;
const int DO_Stacker_Frente = 24;
const int DO_Stacker_Tras = 25;
const int DO_Stacker_Sobe = 26;
const int DO_Stackers_Desce = 27;
const int DO_Stacker_Estender = 28;
const int DO_Stacker_Retrair = 29;
const int DO_WorkPartDestructorPress = 63;

//// DIGITAL INPUTS ////
const int DI_PhotocellLSwConveyor_Signal = 0;
const int DI_PhotocellTop_Signal = 1;
const int DI_PhotocellBottom_Signal = 2;
const int DI_CapacitiveDeep_Signal = 3;
const int DI_CapacitiveInduc_Signal = 4;
const int DI_CapacitiveColor_Signal = 5;
const int DI_Capacitive01_Signal = 6;
const int DI_Capacitive02_Signal = 7;
const int DI_Capacitive03_Signal = 8;
const int DI_PneumaticPushTop_Advanced = 9;
const int DI_PneumaticPushTop_Back = 10;
const int DI_PneumaticPushBottom_Advanced = 11;
const int DI_PneumaticPushBottom_Back = 12;
const int DI_PneumaticPush01_Advanced = 13;
const int DI_PneumaticPush01_Back = 14;
const int DI_PneumaticPush02_Advanced = 15;
const int DI_PneumaticPush02_Back = 16;
const int DI_PneumaticPush04_Advanced = 17;
const int DI_PneumaticPush04_Back = 18;
const int DI_Braco_Vacuo = 19;
const int DI_Braco_Levantado = 20;
const int DI_Braco_Abaixado = 21;
const int DI_Braco_Estendido = 22;
const int DI_Braco_Recuado = 23;
const int DI_Braco_Encoder = 24;
const int DI_Braco_InductSens = 25;
const int DI_Prensa_CestoEst = 26;
const int DI_Prensa_CestoRec = 27;
const int DI_Prensa_PrensaOn = 28;
const int DI_Prensa_PrensaOff = 29;
const int DI_Prensa_PortaFech = 30;
const int DI_Prensa_PortaAbert = 31;
const int DI_Stacker_LimSwitch0 = 32;
const int DI_Stacker_LimSwitch1 = 33;
const int DI_Stacker_LimSwitch2 = 34;
const int DI_Stacker_LimSwitch3 = 35;
const int DI_Stacker_Enconder0 = 36;
const int DI_Stacker_Enconder1 = 37;
const int DI_Stacker_Enconder2 = 38;


bool auxPress = false;
bool automatico = false;
bool aleatorio = false;

int intervalo = 2500;
long ultimoTempo = 0;

int baseType = 0;
int topType = 4;


public void Init()
{
	HMI.ButtonClick += ButtonClick;
	HMI.HMITittle = "Planta de Processos - LCA";
	
	for (int i = 0; i <= 17; i++)
	{
		HMI.TextLines(i, "");
		HMI.ButtonText(i, "");
	}
	
	HMI.TextLines(0, "Topo Metalica");
	HMI.TextLines(1, "Topo Branca");
	HMI.TextLines(2, "Topo Preta");
	HMI.TextLines(12, "Base Metalico");
	HMI.TextLines(13, "Base Branco");
	HMI.TextLines(14, "Base Preto");
	
	HMI.ButtonText(4,"TM");
	HMI.ButtonText(5,"TB");
	HMI.ButtonText(6,"TP");
	HMI.ButtonText(10,"BM");
	HMI.ButtonText(11,"BB");
	HMI.ButtonText(12,"BP");
	
	HMI.TextLines(10, "MODO MANUAL");
	
	HMI.ButtonText(1,"MANUAL");
	HMI.ButtonText(2,"AUTOMATICO");
	HMI.ButtonText(3,"ALEATORIO");
}

public void Main()
{

	if(PhotocellPress.Status && IOManager.GetOutput(DO_Prensa_pistPrenExt))
	{
		IOManager.SetVirtualOutput(DO_WorkPartDestructorPress, true);
		auxPress = true;
	}
	else
	{
		IOManager.SetVirtualOutput(DO_WorkPartDestructorPress, false);
		if(auxPress && !IOManager.GetOutput(DO_Prensa_pistPrenExt))
		{
			WorkPartCreatorPress.CreateNewWorkPart();
			auxPress = false;
		}
	}
	
	if(!CapacitiveDeepTop.Status && !CapacitiveDeepBase.Status)
	{
		IOManager.SetAInput(0, 0);
	}
	
	if(aleatorio)
	{
	    // Este código será executado em loop automaticamente pelo software
	    long tempoAtual = Environment.TickCount;
	
	    // Verifica se já se passaram 5 segundos
	    if (tempoAtual - ultimoTempo >= intervalo)
	    {
	        WorkPartCreatorBase.CreateNewWorkPart(baseType);  
	        WorkPartCreatorTop.CreateNewWorkPart(topType);
	        
	        baseType = UnityEngine.Random.Range(0, 3);
			topType = UnityEngine.Random.Range(3, 6);
	        
	        // Atualiza o último tempo registrado
	        ultimoTempo = tempoAtual;
	    }		
	}
	else if(automatico)
	{
	    // Este código será executado em loop automaticamente pelo software
	    long tempoAtual = Environment.TickCount;
	
	    // Verifica se já se passaram 5 segundos
	    if (tempoAtual - ultimoTempo >= intervalo)
	    {
	        baseType = 0;
			topType = 4;
			
	        WorkPartCreatorBase.CreateNewWorkPart(baseType);  
	        WorkPartCreatorTop.CreateNewWorkPart(topType);
	        
	        // Atualiza o último tempo registrado
	        ultimoTempo = tempoAtual;
	    }
	}
}


void ButtonClick(int buttonNo)
{	
	switch (buttonNo)
	{
		case 1:
			HMI.TextLines(10, "MODO MANUAL");
			automatico = false;
			aleatorio = false;
		break;
		
		case 2:
			HMI.TextLines(10, "MODO AUTOMATICO");
			automatico = true;
			aleatorio = false;
		break;
		
		case 3:
			HMI.TextLines(10, "MODO ALEATORIO");
			automatico = false;
			aleatorio = true;
		break;
		
		default:
		
		break;
	}
	
	if (!automatico && !aleatorio)
	{
		switch (buttonNo)
		{	
			case 4:
				WorkPartCreatorTop.CreateNewWorkPart(3);
			break;
			
			case 5:
				WorkPartCreatorTop.CreateNewWorkPart(4);
			break;
			
			case 6:
				WorkPartCreatorTop.CreateNewWorkPart(5);
			break;
			
			case 10:
				WorkPartCreatorBase.CreateNewWorkPart(0);
			break;
			
			case 11:
				WorkPartCreatorBase.CreateNewWorkPart(1);
			break;
			
			case 12:
				WorkPartCreatorBase.CreateNewWorkPart(2);
			break;
			
			default:
			
			break;
		}
	}
	else
	{
		
	}
}


public void Physics()
{
	
}

public void Finish()
{
	HMI.ButtonClick -= ButtonClick;

}
