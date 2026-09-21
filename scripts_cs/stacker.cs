// =============================================================================
//  stacker.cs  --  Script do UDC do subsistema de armazenamento
// -----------------------------------------------------------------------------
//  A torre movel percorre dois eixos acionados por motor e carrega um garfo
//  pneumatico que insere e retira os cubos das prateleiras.
//
//  Diferenca em relacao ao braco e a prensa: os eixos NAO sao animados ate uma
//  posicao predefinida. A cada ciclo a rotina le a posicao atual e aplica um
//  pequeno incremento na direcao comandada, de modo que o deslocamento
//  prossegue enquanto o comando estiver ativo e cessa assim que ele e retirado
//  -- o comportamento de um motor, e nao o de um cilindro.
//
//  Fotocelulas (os nomes dos componentes internos nao foram alterados):
//    Photocell00 -> AtLoadPosition, posicao de recebimento do cubo vindo do braco
//    Photocell01 -> EncoderH, pulsos do eixo horizontal
//    Photocell02 -> EncoderV, pulsos do eixo vertical
//  Como no braco, o componente apenas gera os pulsos; a contagem cabe ao CLP.
//
//  Atencao: o campo Description da interface do UDC aceita no maximo 10
//  caracteres, entao alguns nomes ficam truncados ("MoveForwar", "LimitForwa",
//  "AtLoadPosi"). As strings abaixo precisam bater exatamente com o que esta
//  declarado no componente, truncamento incluido.
// =============================================================================

//// DIGITAL OUTPUTS ////  (comandos que o CLP envia ao componente)
const string DO_MoveForward = "MoveForwar";
const string DO_MoveBack = "MoveBack";
const string DO_MoveUp = "MoveUp";
const string DO_MoveDown = "MoveDown";
const string DO_Extend = "Extend";
const string DO_Retract = "Retract";

//// DIGITAL INPUTS ////  (retornos que o componente envia ao CLP)
const string DI_LimitBack = "LimitBack";   // limite do movimento para tras
const string DI_LimitForward = "LimitForwa";   // limite do movimento para frente
const string DI_LimitDown = "LimitDown";   // limite do movimento para baixo
const string DI_LimitUp = "LimitUp";   // limite do movimento para cima
const string DI_AtLoadPosition = "AtLoadPosi";     // posicao de recebimento do cubo
const string DI_EncoderH = "EncoderH";     // pulsos do eixo horizontal
const string DI_EncoderV = "EncoderV";     // pulsos do eixo vertical
const string DI_Extended = "Extended";
const string DI_Retracted = "Retracted";

// Posicoes extremas do garfo pneumatico
const float retracted = 0.5f;
const float extended = 1.2f;

// Incremento aplicado a cada ciclo em cada direcao. O sinal define o sentido.
const float frente = -0.01f;
const float tras = 0.01f;
const float sobe = 0.01f;
const float desce = -0.01f;

const float speedZ = 0.2f;        // velocidade do eixo horizontal
const float speedY = 0.2f;        // velocidade do eixo vertical
const float speedPiston = 0.5f;   // velocidade do garfo

const string Y = "Y";   // eixo vertical
const string Z = "Z";   // eixo horizontal

bool condHook;

// Estado da verificacao de esforco em cada fim de curso: o instante em que a
// chave foi atingida e o travamento que impede o efeito de reaparecer.
DateTime timeLS00 = DateTime.MinValue;
DateTime timeLS01 = DateTime.MinValue;
DateTime timeLS02 = DateTime.MinValue;
DateTime timeLS03 = DateTime.MinValue;
bool condSw00;
bool condSw01;
bool condSw02;
bool condSw03;

/////////////////////////////////////////////////////////////////////////////////////////////////////

public void Init()
{

}

public void Main()
{
	// --- Retornos ao CLP -----------------------------------------------------

	UC.SetInput(DI_LimitBack, LimitSwitch00.Status);
	UC.SetInput(DI_LimitForward, LimitSwitch01.Status);
	UC.SetInput(DI_LimitDown, LimitSwitch02.Status);
	UC.SetInput(DI_LimitUp, LimitSwitch03.Status);

	UC.SetInput(DI_AtLoadPosition, Photocell00.Status);
	UC.SetInput(DI_EncoderH, Photocell01.Status);
	UC.SetInput(DI_EncoderV, Photocell02.Status);


	// --- Esforco desnecessario sobre os fins de curso ------------------------
	// Cada chave tem o seu proprio efeito, de modo que se identifica qual dos
	// quatro limites foi forcado.

	LimitSwitchStress(UC.GetOutput(DO_MoveBack),ref condSw00,ref timeLS00,LimitSwitch00,SmokeFX00);
	LimitSwitchStress(UC.GetOutput(DO_MoveForward),ref condSw01,ref timeLS01,LimitSwitch01,SmokeFX01);
	LimitSwitchStress(UC.GetOutput(DO_MoveDown),ref condSw02,ref timeLS02,LimitSwitch02,SmokeFX02);
	LimitSwitchStress(UC.GetOutput(DO_MoveUp),ref condSw03,ref timeLS03,LimitSwitch03,SmokeFX03);

	// --- Garfo ---------------------------------------------------------------
	// O cubo fica preso ao garfo enquanto ele estiver parado ou enquanto a
	// fotocelula do eixo vertical estiver acionada.

	condHook = !Piston.IsMoving || Photocell02.Status;

	if(Piston.CurrPos()[2] == retracted)
	{
		UC.SetInput(DI_Retracted, true);
		UC.SetInput(DI_Extended, false);
	}
	else if(Piston.CurrPos()[2] == extended)
	{
		UC.SetInput(DI_Retracted, false);
		UC.SetInput(DI_Extended, true);
	}
	else
	{
		// Em curso: nenhum dos dois sinais ativo
		UC.SetInput(DI_Retracted, false);
		UC.SetInput(DI_Extended, false);
	}

	if(condHook)
	{
		Hook.Pick(true);
	}
	else
	{
		Hook.Pick(false);
	}

	// --- Eixos ---------------------------------------------------------------
	// Cada movimento exige o seu comando ativo, o comando oposto inativo e a
	// chave de fim de curso correspondente livre.

	if(UC.GetOutput(DO_MoveBack) && !LimitSwitch00.Status && !UC.GetOutput(DO_MoveForward))
	{
		MoveZ(tras);
	}

	if(UC.GetOutput(DO_MoveForward) && !LimitSwitch01.Status && !UC.GetOutput(DO_MoveBack))
	{
		MoveZ(frente);
	}

	if(UC.GetOutput(DO_MoveDown) && !LimitSwitch02.Status && !UC.GetOutput(DO_MoveUp))
	{
		MoveY(desce);
	}

	if(UC.GetOutput(DO_MoveUp) && !LimitSwitch03.Status && !UC.GetOutput(DO_MoveDown))
	{
		MoveY(sobe);
	}

	// --- Garfo pneumatico ----------------------------------------------------
	// Cilindro de acao simples, mas com avanco e recuo comandados
	// explicitamente pelo programa de controle.

	if(UC.GetOutput(DO_Extend) && !UC.GetOutput(DO_Retract))
	{
		Piston.AnimationMove(Z,speedPiston,extended);
	}

	if(UC.GetOutput(DO_Retract) && !UC.GetOutput(DO_Extend))
	{
		Piston.AnimationMove(Z,speedPiston,retracted);
	}
}

public void Physics()
{

}

public void Finish()
{

}

// -----------------------------------------------------------------------------
//  MoveZ / MoveY -- deslocamento incremental dos eixos.
//
//  Le a posicao atual do conjunto e aplica um incremento na direcao comandada.
//  E o que reproduz o motor: o eixo anda enquanto o comando existir e para onde
//  estiver quando ele cessar.
//
//  MoveZ desloca o MasterRod, que e o conjunto pai e carrega toda a torre.
//  MoveY desloca apenas o Cylinder, o carro vertical.
// -----------------------------------------------------------------------------
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

// -----------------------------------------------------------------------------
//  LimitSwitchStress -- efeito de esforco sobre um fim de curso.
//
//  Enquanto a chave nao esta acionada, o instante de referencia e atualizado a
//  cada ciclo. Assim que ela e atingida o relogio passa a correr: se o comando
//  de deslocamento permanecer ativo por mais de um segundo, o efeito aparece.
//  'cond' impede que ele seja reacionado sem que a chave seja liberada antes.
// -----------------------------------------------------------------------------
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
