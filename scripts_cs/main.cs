// =============================================================================
//  main.cs  --  Script da maquina (cenario completo)
// -----------------------------------------------------------------------------
//  Enquanto os scripts dos UDCs tratam do comportamento interno de cada
//  equipamento, este trata do que nao pertence a nenhum deles isoladamente:
//    - a formacao do cubo montado, por troca de objetos na prensa;
//    - o painel de operacao e os tres modos de criacao de pecas;
//    - o zeramento da porta analogica dos sensores de profundidade.
//
//  As constantes abaixo sao os NUMEROS de entrada e saida do simulador, que o
//  driver de comunicacao associa aos enderecos do CLP. Nem todas sao usadas
//  neste script: a lista esta completa para servir de referencia do mapeamento.
// =============================================================================

//// DIGITAL OUTPUTS ////
const int DO_SelConveyor_Advance = 0;
const int DO_SelFeederTop_Advance = 1;
const int DO_SelFeederTop_Back = 2;
const int DO_SelFeederBase_Advance = 3;
const int DO_SelFeederBase_Back = 4;
const int DO_SelSorter01_Advance = 5;
// saida 6 livre: recuo do atuador de acao simples, sem uso
const int DO_SelSorter02_Advance = 7;
// saida 8 livre: recuo do atuador de acao simples, sem uso
const int DO_SelSorter03_Advance = 9;
// saida 10 livre: recuo do atuador de acao simples, sem uso
const int DO_ArmUnit_Vacuum = 11;
const int DO_ArmUnit_Raise = 12;
const int DO_ArmUnit_Lower = 13;
const int DO_ArmUnit_Extend = 14;
const int DO_ArmUnit_Retract = 15;
const int DO_ArmUnit_RotateCW = 16;
const int DO_ArmUnit_RotateCCW = 17;
const int DO_PressUnit_BasketExtend = 18;
const int DO_PressUnit_BasketRetract = 19;
const int DO_PressUnit_DoorClose = 20;
const int DO_PressUnit_DoorOpen = 21;
const int DO_PressUnit_PressExtend = 22;
const int DO_PressUnit_PressRetract = 23;
const int DO_StackUnit_MoveForward = 24;
const int DO_StackUnit_MoveBack = 25;
const int DO_StackUnit_MoveUp = 26;
const int DO_StackUnit_MoveDown = 27;
const int DO_StackUnit_Extend = 28;
const int DO_StackUnit_Retract = 29;
const int DO_SelConveyor_Reverse = 30;

// Saida virtual: nao vai ao CLP, e escrita por este script para acionar o
// destrutor de pecas instalado no interior da prensa.
const int DO_WorkPartDestructorPress = 63;

//// DIGITAL INPUTS ////
const int DI_SelPhotocellEnd_Signal = 0;
const int DI_SelPhotocellTop_Signal = 1;
const int DI_SelPhotocellBase_Signal = 2;
const int DI_SelSensorDepth_Signal = 3;   // compartilhada pelos dois sensores de profundidade
const int DI_SelSensorInduc_Signal = 4;
const int DI_SelSensorColor_Signal = 5;
const int DI_SelSorterSensor01_Signal = 6;
const int DI_SelSorterSensor02_Signal = 7;
const int DI_SelSorterSensor03_Signal = 8;
const int DI_SelFeederTop_Advanced = 9;
const int DI_SelFeederTop_Back = 10;
const int DI_SelFeederBase_Advanced = 11;
const int DI_SelFeederBase_Back = 12;
const int DI_SelSorter01_Advanced = 13;
const int DI_SelSorter01_Back = 14;
const int DI_SelSorter02_Advanced = 15;
const int DI_SelSorter02_Back = 16;
const int DI_SelSorter03_Advanced = 17;
const int DI_SelSorter03_Back = 18;
const int DI_ArmUnit_VacuumOn = 19;
const int DI_ArmUnit_Raised = 20;
const int DI_ArmUnit_Lowered = 21;
const int DI_ArmUnit_Extended = 22;
const int DI_ArmUnit_Retracted = 23;
const int DI_ArmUnit_EncoderPulse = 24;
const int DI_ArmUnit_HomeSensor = 25;
// Atencao: a ordem das entradas do UDC da prensa mudou com a renomeacao.
// A porta passou a ocupar os indices 2 e 3, e a prensa, os indices 4 e 5.
const int DI_PressUnit_BasketExtended = 26;
const int DI_PressUnit_BasketRetracted = 27;
const int DI_PressUnit_DoorClosed = 28;
const int DI_PressUnit_DoorOpened = 29;
const int DI_PressUnit_PressExtended = 30;
const int DI_PressUnit_PressRetracted = 31;
const int DI_StackUnit_LimitBack = 32;
const int DI_StackUnit_LimitForward = 33;
const int DI_StackUnit_LimitDown = 34;
const int DI_StackUnit_LimitUp = 35;
const int DI_StackUnit_AtLoadPosition = 36;
const int DI_StackUnit_EncoderH = 37;
const int DI_StackUnit_EncoderV = 38;
const int DI_StackUnit_Extended = 39;
const int DI_StackUnit_Retracted = 40;


// Registro de prensagem: marcado no instante em que as duas metades sao
// destruidas e consultado nos ciclos seguintes, para que o cubo seja criado uma
// unica vez e somente se as pecas foram de fato removidas.
bool auxPress = false;

bool automatico = false;
bool aleatorio = false;

int intervalo = 2500;      // intervalo entre criacoes nos modos automaticos, em ms
long ultimoTempo = 0;      // instante da ultima criacao

int baseType = 0;          // tipo da proxima base a ser criada
int topType = 4;           // tipo do proximo topo a ser criado


public void Init()
{
	HMI.ButtonClick += ButtonClick;
	HMI.HMITittle = "Planta de Processos - LCA";

	// O layout do painel e fixo; ao script cabe apenas atribuir os textos.
	// Comeca limpando todas as linhas e botoes disponiveis.
	for (int i = 0; i <= 17; i++)
	{
		HMI.TextLines(i, "");
		HMI.ButtonText(i, "");
	}

	// Linhas de texto que identificam os botoes laterais
	HMI.TextLines(0, "Topo Metalica");
	HMI.TextLines(1, "Topo Branca");
	HMI.TextLines(2, "Topo Preta");
	HMI.TextLines(12, "Base Metalico");
	HMI.TextLines(13, "Base Branco");
	HMI.TextLines(14, "Base Preto");

	// Botoes de criacao individual: topos a esquerda, bases a direita
	HMI.ButtonText(4,"TM");
	HMI.ButtonText(5,"TB");
	HMI.ButtonText(6,"TP");
	HMI.ButtonText(10,"BM");
	HMI.ButtonText(11,"BB");
	HMI.ButtonText(12,"BP");

	// Linha central: modo de operacao em vigor
	HMI.TextLines(10, "MODO MANUAL");

	// Botoes inferiores: selecao do modo
	HMI.ButtonText(1,"MANUAL");
	HMI.ButtonText(2,"AUTOMATICO");
	HMI.ButtonText(3,"ALEATORIO");
}

public void Main()
{
	// --- Formacao do cubo montado -------------------------------------------
	// O simulador nao une duas WorkParts. A montagem e reproduzida por uma troca
	// de objetos, dividida em dois instantes: o destrutor apaga a base e o topo
	// enquanto a prensa esta avancada sobre eles, e o gerador cria o cubo so
	// depois, quando o comando de avanco e retirado -- caso contrario o cubo
	// surgiria atravessando o cilindro.

	if(PhotocellPress.Status && IOManager.GetOutput(DO_PressUnit_PressExtend))
	{
		IOManager.SetVirtualOutput(DO_WorkPartDestructorPress, true);
		auxPress = true;
	}
	else
	{
		IOManager.SetVirtualOutput(DO_WorkPartDestructorPress, false);
		if(auxPress && !IOManager.GetOutput(DO_PressUnit_PressExtend))
		{
			WorkPartCreatorPress.CreateNewWorkPart();
			auxPress = false;
		}
	}

	// --- Porta analogica do sensor de profundidade ---------------------------
	// Os dois sensores escrevem valores distintos na mesma porta analogica. O
	// valor permanece depois que a peca passa, entao a porta e zerada sempre que
	// nenhum dos dois esta acionado: ausencia de peca corresponde a leitura nula.

	if(!SelSensorDepthTop.Status && !SelSensorDepthBase.Status)
	{
		IOManager.SetAInput(0, 0);
	}

	// --- Modos automaticos de criacao de pecas -------------------------------

	if(aleatorio)
	{
		long tempoAtual = Environment.TickCount;

		// Verifica se ja se passou o intervalo definido em 'intervalo'
		if (tempoAtual - ultimoTempo >= intervalo)
		{
			// Cria o par sorteado na iteracao anterior e ja sorteia o proximo.
			// Por isso o primeiro par do modo aleatorio e sempre 0 e 4.
			SelCreatorBase.CreateNewWorkPart(baseType);
			SelCreatorTop.CreateNewWorkPart(topType);

			baseType = UnityEngine.Random.Range(0, 3);   // 0, 1 ou 2: bases
			topType = UnityEngine.Random.Range(3, 6);    // 3, 4 ou 5: topos

			ultimoTempo = tempoAtual;
		}
	}
	else if(automatico)
	{
		long tempoAtual = Environment.TickCount;

		if (tempoAtual - ultimoTempo >= intervalo)
		{
			// Par que atende a especificacao de producao:
			// base metalica (0) e topo branco (4)
			baseType = 0;
			topType = 4;

			SelCreatorBase.CreateNewWorkPart(baseType);
			SelCreatorTop.CreateNewWorkPart(topType);

			ultimoTempo = tempoAtual;
		}
	}
}


// -----------------------------------------------------------------------------
//  ButtonClick -- evento disparado pelo painel com o numero do botao apertado.
// -----------------------------------------------------------------------------
void ButtonClick(int buttonNo)
{
	// Botoes inferiores: trocam o modo de operacao
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

	// Botoes laterais: criacao individual, ignorada fora do modo manual, para
	// que nao interfira na cadencia do modo em execucao.
	if (!automatico && !aleatorio)
	{
		switch (buttonNo)
		{
			case 4:
				SelCreatorTop.CreateNewWorkPart(3);    // topo metalico
			break;

			case 5:
				SelCreatorTop.CreateNewWorkPart(4);    // topo branco
			break;

			case 6:
				SelCreatorTop.CreateNewWorkPart(5);    // topo preto
			break;

			case 10:
				SelCreatorBase.CreateNewWorkPart(0);   // base metalica
			break;

			case 11:
				SelCreatorBase.CreateNewWorkPart(1);   // base branca
			break;

			case 12:
				SelCreatorBase.CreateNewWorkPart(2);   // base preta
			break;

			default:

			break;
		}
	}
}


public void Physics()
{

}

public void Finish()
{
	HMI.ButtonClick -= ButtonClick;

}
