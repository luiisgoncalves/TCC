// =============================================================================
//  arm.cs  --  Script do UDC do subsistema de manipulacao (braco robotico)
// -----------------------------------------------------------------------------
//  Reproduz o comportamento do braco de tres graus de liberdade:
//    - rotacao da base, comandada pelos dois sentidos de giro;
//    - elevacao vertical e extensao horizontal, por cilindros de acao simples;
//    - captura de pecas pela ventosa (componente Hook).
//
//  O componente apenas GERA os pulsos do encoder. A contagem e a deducao da
//  posicao angular sao responsabilidade do programa de controle no CLP, do
//  mesmo modo que ocorre na planta real.
//
//  Os cilindros sao de acao simples: a haste avanca enquanto o comando estiver
//  ativo e retorna sozinha a posicao de repouso quando ele e retirado. Por isso
//  MovePiston recebe um unico sinal de comando de avanco.
// =============================================================================

//  O campo Description da interface do UDC aceita no maximo 10 caracteres:
//  "EncoderPul" esta truncado e precisa ser escrito assim.
//
//// DIGITAL OUTPUTS ////  (comandos que o CLP envia ao componente)
const string DO_Vacuum = "Vacuum";
const string DO_Raise = "Raise";
const string DO_Lower = "Lower";
const string DO_Extend = "Extend";
const string DO_Retract = "Retract";
const string DO_RotateCW = "RotateCW";
const string DO_RotateCCW = "RotateCCW";

//// DIGITAL INPUTS ////  (retornos que o componente envia ao CLP)
const string DI_VacuumOn = "VacuumOn";
const string DI_HomeSensor = "HomeSensor";
const string DI_Extended = "Extended";
const string DI_Retracted = "Retracted";
const string DI_Raised = "Raised";
const string DI_Lowered = "Lowered";
const string DI_EncoderPulse = "EncoderPul";

// Sentidos aceitos por AnimationRotate
const int horario = 0;
const int antihor = 1;

// Eixos de referencia dentro do componente
const string axisY = "Y";   // eixo de giro da base
const string axisZ = "Z";   // eixo de curso dos dois cilindros

const float rotBase = 180;        // curso angular maximo da base, em graus
const float speedRotBase = 10f;   // velocidade de giro da base
const float speedPiston = 0.6f;   // velocidade de avanco das hastes
const float volumeSoundFX = 1f;   // volume do ruido de vacuo

// Posicoes extremas de cada cilindro, no eixo local
const float horizontalInit = 0.3f;   // haste horizontal recuada
const float horizontalTarg = 1f;     // haste horizontal estendida
const float verticalInit = 0.5f;     // haste vertical abaixada
const float verticalTarg = 1.5f;     // haste vertical levantada

public void Init()
{
	// Faiscas no efeito de fumaca usado para sinalizar comando contraditorio
	SmokeFX.Embers = true;
}

public void Main()
{
	// --- Cilindros -----------------------------------------------------------
	// A mesma rotina serve aos dois, mudando apenas os sinais e as posicoes.
	MovePiston(VerticalPiston,DO_Raise,DO_Lower,axisZ,speedPiston,verticalInit,verticalTarg,DI_Raised,DI_Lowered);
	MovePiston(HorizontalPiston,DO_Extend,DO_Retract,axisZ,speedPiston,horizontalInit,horizontalTarg,DI_Extended,DI_Retracted);

	// --- Sensores da base ----------------------------------------------------
	// InductiveSwitch        -> pino de referencia, uma deteccao por volta
	// InductiveSwitchENCODER -> passa diante dos blocos do anel, gerando pulsos
	UC.SetInput(DI_HomeSensor,InductiveSwitch.Status);
	UC.SetInput(DI_EncoderPulse,InductiveSwitchENCODER.Status);

	// --- Rotacao da base -----------------------------------------------------
	// Girar apenas a Base basta: por parenteamento, todo o braco a acompanha.
	if (UC.GetOutput(DO_RotateCW) && !UC.GetOutput(DO_RotateCCW))
	{
		Base.AnimationRotate(axisY,rotBase,speedRotBase,horario);
	}

	else if (UC.GetOutput(DO_RotateCCW) && !UC.GetOutput(DO_RotateCW))
	{
		Base.AnimationRotate(axisY,rotBase,speedRotBase,antihor);
	}

	else
	{
		// Nenhum comando, ou os dois ao mesmo tempo: movimento interrompido
		Base.AnimationRotate(axisY,0,speedRotBase,antihor);
	}

	// Comando contraditorio: os dois sentidos acionados juntos. O movimento ja
	// foi interrompido acima; a fumaca existe para que a causa fique visivel.
	if(UC.GetOutput(DO_RotateCW) && UC.GetOutput(DO_RotateCCW))
	{
		SmokeFX.Visible = true;

	}
	else
	{
		SmokeFX.Visible = false;
	}

	// --- Ventosa -------------------------------------------------------------
	// O retorno DI_VacuumOn confirma ao CLP que o vacuo esta acionado.
	if (UC.GetOutput(DO_Vacuum))
	{
		Hook.Pick(true);
		UC.SetInput(DI_VacuumOn, true);
		EditorUtils.PlayLoopSound("Suction", volumeSoundFX);
	}
	else
	{
		Hook.Pick(false);
		UC.SetInput(DI_VacuumOn, false);
		EditorUtils.StopSound();
	}
}

public void Physics()
{

}

public void Finish()
{

}

// -----------------------------------------------------------------------------
//  MovePiston -- cilindro de acao simples.
//
//  Enquanto outputExt estiver ativo a haste e animada ate posTarg; na ausencia
//  do comando, retorna a posInit. Em seguida a posicao atual e comparada com as
//  duas extremas e os sinais de fim de curso sao atualizados.
//
//  outputRec entra na assinatura por simetria com a rotina da prensa, mas nao e
//  utilizado aqui: o retorno do cilindro de acao simples e automatico.
// -----------------------------------------------------------------------------
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
