// =============================================================================
//  press.cs  --  Script do UDC do subsistema de prensa
// -----------------------------------------------------------------------------
//  Reune os tres cilindros de dupla acao da prensa:
//    - cesto : desloca as pecas para dentro e para fora da camara;
//    - porta : abre e fecha a camara;
//    - prensa: une a base e o topo do cubo.
//
//  Diferenca em relacao ao braco: aqui avanco e recuo sao comandos
//  independentes, o que da tres situacoes possiveis -- avanca, recua ou
//  permanece onde esta (com os dois comandos ativos ou com nenhum).
//
//  A formacao do cubo em si NAO pertence a este script: quem troca as duas
//  metades pelo cubo montado e o script da maquina (main.cs).
// =============================================================================

//  O campo Description da interface do UDC aceita no maximo 10 caracteres, de
//  modo que "BasketExtend" e "BasketExtended" ficam os dois como "BasketExte".
//  Nao ha conflito: entradas e saidas do UDC sao listas separadas, e o que
//  distingue as duas e a chamada, UC.GetOutput ou UC.SetInput.
//
//// DIGITAL OUTPUTS ////  (comandos que o CLP envia ao componente)
const string DO_BasketExtend = "BasketExte";
const string DO_BasketRetract = "BasketRetr";
const string DO_PressExtend = "PressExten";
const string DO_PressRetract = "PressRetra";
const string DO_DoorClose = "DoorClose";
const string DO_DoorOpen = "DoorOpen";

//// DIGITAL INPUTS ////  (retornos que o componente envia ao CLP)
const string DI_BasketExtended = "BasketExte";
const string DI_BasketRetracted = "BasketRetr";
const string DI_PressExtended = "PressExten";
const string DI_PressRetracted = "PressRetra";
const string DI_DoorClosed = "DoorClosed";
const string DI_DoorOpened = "DoorOpened";

// Cesto: estendido = fora da camara
float actuatorPosInit = 0.2f;
float actuatorPosTarg = 1.3f;
float actuatorMovSpeed = 1f;
string actuatorDirecAxis = "Z";

// Prensa: estendida = pecas sendo comprimidas
float pressPosInit = 0.1f;
float pressPosTarg = 0.5f;
float pressMovSpeed = 2f;
string pressDirecAxis = "Z";

// Porta: estendida = fechada
float doorPosInit = -0.11f;
float doorPosTarg = -0.5f;
float doorMovSpeed = 1f;
string doorDirecAxis = "Z";

public void Init()
{

}

public void Main()
{
	VerifyCollision();

	// Os tres cilindros usam a mesma rotina. Note a ordem dos dois ultimos
	// argumentos: MovePiston recebe primeiro o sensor da posicao recuada.
	MovePiston(PistonHor,DO_BasketExtend,DO_BasketRetract,actuatorDirecAxis,actuatorMovSpeed,actuatorPosInit,actuatorPosTarg,DI_BasketRetracted,DI_BasketExtended);
	MovePiston(PistonPress,DO_PressExtend,DO_PressRetract,pressDirecAxis,pressMovSpeed,pressPosInit,pressPosTarg,DI_PressRetracted,DI_PressExtended);
	MovePiston(PistonDoor,DO_DoorClose,DO_DoorOpen,doorDirecAxis,doorMovSpeed,doorPosInit,doorPosTarg,DI_DoorOpened,DI_DoorClosed);

	// Ruido pneumatico: soa enquanto qualquer um dos tres cilindros estiver
	// entre as duas posicoes extremas, ou seja, em movimento.
	if(!(UC.GetInput(DI_BasketExtended) || UC.GetInput(DI_BasketRetracted)) ||
	   !(UC.GetInput(DI_PressExtended) || UC.GetInput(DI_PressRetracted)) ||
	   !(UC.GetInput(DI_DoorClosed) || UC.GetInput(DI_DoorOpened)))
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

// -----------------------------------------------------------------------------
//  VerifyCollision -- sinaliza interferencia mecanica.
//
//  O cesto, a porta e o cilindro de prensagem disputam a mesma regiao. O cesto
//  so pode estar estendido com a porta aberta e a prensa recolhida. Qualquer
//  outra combinacao corresponderia a uma colisao na planta real e aciona o
//  efeito de explosao. O modelo nao e interrompido: o efeito apenas torna o
//  comando indevido visivel a quem opera.
// -----------------------------------------------------------------------------
public void VerifyCollision()
{
	if(UC.GetInput(DI_BasketExtended) && UC.GetInput(DI_PressExtended) || UC.GetInput(DI_BasketExtended) && UC.GetInput(DI_DoorClosed))
	{
		ExplosionFX.Visible = true;
	}
	else
	{
		ExplosionFX.Visible = false;
	}
}

// -----------------------------------------------------------------------------
//  MovePiston -- cilindro de dupla acao.
//
//  Avanca com pistonExt sozinho, recua com pistonRec sozinho e permanece na
//  posicao atual quando ambos ou nenhum estao ativos. A atualizacao dos sinais
//  de fim de curso e identica a do braco, pela comparacao da posicao atual com
//  as duas posicoes extremas.
// -----------------------------------------------------------------------------
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
