# Resultados dos testes unitários do backend

**Projeto:** CampusCarpool  
**Issue:** I059 — Testes unitários  
**Data do relatório:** 28/10/2026  
**Estado da meta de cobertura:** Atingida (métrica global de linhas)  
**Fonte da evidência:** `coverage.cobertura(10).xml`, gerado pela execução de testes com recolha de cobertura.

## 1. Objetivo

Documentar a evidência de cobertura dos testes do backend CampusCarpool e confirmar se foi atingida a meta global de **pelo menos 80% de cobertura de linhas**. O presente relatório refere-se à atividade de testes unitários da I059, mas o ficheiro Cobertura agrega a execução do conjunto de testes e **não separa o contributo dos testes unitários do contributo dos testes de integração**.

## 2. Ambiente e procedimento de recolha

- Plataforma de desenvolvimento: .NET 10 / ASP.NET Core Web API.
- Framework de testes utilizada no projeto: xUnit.
- Formato de cobertura: Cobertura XML (`XPlat Code Coverage`).
- Configuração de seleção de ficheiros: `coverage.runsettings`.

Comando de referência, a partir da pasta que contém `CampusCarpool.slnx`:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --results-directory .\artifacts\cobertura-atualizada
```

**Nota:** o XML comprova os valores da recolha de cobertura, mas não inclui por si só a listagem de testes aprovados/falhados; para comprovar individualmente o sucesso de todos os testes, devem ser anexados os resultados do `dotnet test` (por exemplo, log ou TRX).

## 3. Resultados globais registados

| Indicador | Resultado |
|---|---:|
| Linhas analisadas | 1 737 |
| Linhas cobertas | 1 467 |
| Linhas não cobertas | 270 |
| **Cobertura de linhas** | **84,45%** |
| Ramos analisados | 705 |
| Ramos cobertos | 563 |
| Cobertura de ramos | 79,85% |
| Meta estabelecida para linhas | ≥ 80% |
| **Meta global de linhas** | **Atingida** |

A cobertura global excede a meta em **4,45 pontos percentuais**. A cobertura de ramos fica ligeiramente abaixo dos 80%; não deve ser confundida com a métrica de linhas.

## 4. Cobertura por projeto (inclui testes unitários e de integração)

| Projeto | Cobertura de linhas | Cobertura de ramos |
|---|---:|---:|
| CampusCarpool.Api | 82,42% | 80,41% |
| CampusCarpool.Application | 85,99% | 71,59% |
| CampusCarpool.Domain | 100,00% | 100,00% |
| CampusCarpool.Infrastructure | 90,51% | 88,23% |

A análise reporta cobertura nos serviços e na lógica de domínio/aplicação abrangidos pelos testes executados. **Não é possível atribuir estes valores exclusivamente aos testes unitários** com o XML fornecido.

## 5. Evolução da cobertura

| Recolha | Linhas cobertas / válidas | Cobertura de linhas |
|---|---|---:|
| `coverage.cobertura(9).xml` | 1 383 / 1 737 | 79,62% |
| **`coverage.cobertura(10).xml`** | **1 467 / 1 737** | **84,45%** |

Foram cobertas **mais 84 linhas**, sem alteração do total de linhas analisadas.

## 6. Conclusão e evidência de fecho

**A meta global de cobertura de linhas ≥ 80% está cumprida**, de acordo com `coverage.cobertura(10).xml` (84,45%). O relatório é utilizável como evidência quantitativa da I059, em conjunto com o relatório da I060.

**Evidências recomendadas para anexar à issue:**

1. Ficheiro `coverage.cobertura(10).xml`.
2. Resultado da execução de `dotnet test` com contagem de testes aprovados/falhados e sem erros.
3. Referência ao commit/merge request dos testes unitários, para rastreabilidade.

**Limitação da evidência:** o ficheiro de cobertura não comprova isoladamente a aprovação de todos os testes nem identifica qual a percentagem produzida só pelos testes unitários. O fecho integral da I059 deve ainda observar os restantes critérios de aceitação específicos da issue, se existirem.


## 7. Atualização: execução da versão entregue (06/11/2026)

A tabela seguinte regista a execução da versão entregue em `04.12-backend/m3-v01`. Substitui, como resultado final, os valores das secções anteriores, que ficam como histórico de 28/10/2026.

| Campo | Valor |
| --- | --- |
| Versão testada | Backend entregue em `04.12-backend/m3-v01`, commit `0205ac1` |
| Execução | 06/11/2026 10:30 UTC, pipeline, `dotnet test CampusCarpool.slnx -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --logger trx` |
| Ambiente | GitHub Actions, ubuntu-latest, .NET SDK 10 (`global.json`), SQLite em memória e SQL Server 2022 em contentor |
| Correspondência com a pipeline | Esta é a própria execução da pipeline: 42202915885 do GitHub Actions, com todos os passos verdes. A repetição da suite em SQL Server real deu 673 aprovados, 0 falhados e 1 saltado de propósito (`I110_15`, só faz sentido em SQLite) |
| Indicador | Resultado |
| --- | --- |
| Execuções de teste | 260 |
| Aprovadas | 260 |
| Falhadas | 0 |
| Saltadas | 0 |
| Classes de teste | 24 |

#### Resultados por classe

| Classe | Execuções | Aprovadas | Falhadas | Saltadas |
| --- | ---: | ---: | ---: | ---: |
| `ArquiteturaTests` | 7 | 7 | 0 | 0 |
| `AvaliacaoServiceTests` | 36 | 36 | 0 | 0 |
| `CalculadoraDeCustoAdditionalTests` | 9 | 9 | 0 | 0 |
| `CalculadoraDeCustoTests` | 2 | 2 | 0 | 0 |
| `DispositivosControllerTests` | 9 | 9 | 0 | 0 |
| `EntidadesTests` | 19 | 19 | 0 | 0 |
| `GlobalExceptionHandlerCoverageTests` | 4 | 4 | 0 | 0 |
| `HistoricoControllerTests` | 2 | 2 | 0 | 0 |
| `I051ValidacoesControllersTests` | 32 | 32 | 0 | 0 |
| `I052ParticipacaoServiceTests` | 9 | 9 | 0 | 0 |
| `NotificadorPedidosSignalRTests` | 4 | 4 | 0 | 0 |
| `OrdenadorDePontosTests` | 6 | 6 | 0 | 0 |
| `ParticipacaoTests` | 18 | 18 | 0 | 0 |
| `PedidosUserIdProviderTests` | 3 | 3 | 0 | 0 |
| `RegrasDeConsultaTests` | 8 | 8 | 0 | 0 |
| `RotaServiceTests` | 9 | 9 | 0 | 0 |
| `ServicoEmailBrevoCoverageTests` | 8 | 8 | 0 | 0 |
| `ServicoMapasRotasMatrizTests` | 4 | 4 | 0 | 0 |
| `ServicoMapasRotasTests` | 22 | 22 | 0 | 0 |
| `ServicoPushTests` | 8 | 8 | 0 | 0 |
| `TempoLimiteClientesHttpTests` | 1 | 1 | 0 | 0 |
| `ValidadorEmailAcademicoTests` | 7 | 7 | 0 | 0 |
| `ValidadorEmailFormatoTests` | 8 | 8 | 0 | 0 |
| `ViagemTests` | 25 | 25 | 0 | 0 |

#### Lista integral das execuções

| # | Classe | Método (com parâmetros) | Resultado | ms |
| ---: | --- | --- | --- | ---: |
| 1 | ArquiteturaTests | Api_NaoUsaEntidades | Aprovado | 1 |
| 2 | ArquiteturaTests | Api_NaoUsaOAcessoADados | Aprovado | 1 |
| 3 | ArquiteturaTests | Api_SoConheceARegistoDaInfrastructure | Aprovado | 6 |
| 4 | ArquiteturaTests | Application_DependeSoDoDomainDentroDaSolucao | Aprovado | 1 |
| 5 | ArquiteturaTests | Application_NaoConheceEntityFrameworkNemAspNetCore | Aprovado | 1 |
| 6 | ArquiteturaTests | Domain_NaoDependeDeOutrasCamadasNemDeFrameworks | Aprovado | 0 |
| 7 | ArquiteturaTests | Infrastructure_NaoDependeDaApi | Aprovado | 2 |
| 8 | AvaliacaoServiceTests | Alterar_GravacaoRecusadaPelaBd_Conflito | Aprovado | 1 |
| 9 | AvaliacaoServiceTests | Alterar_ImpedeOutroUtilizadorDeAlterarAvaliacao | Aprovado | 2 |
| 10 | AvaliacaoServiceTests | Alterar_RejeitaClassificacaoInvalida(valor: 0) | Aprovado | 0 |
| 11 | AvaliacaoServiceTests | Alterar_RejeitaClassificacaoInvalida(valor: -2) | Aprovado | 1 |
| 12 | AvaliacaoServiceTests | Alterar_RejeitaClassificacaoInvalida(valor: 6) | Aprovado | 0 |
| 13 | AvaliacaoServiceTests | Alterar_RejeitaQuandoNaoExisteAvaliacaoDoAutor | Aprovado | 1 |
| 14 | AvaliacaoServiceTests | Alterar_SubstituiValorSemCriarNovaAvaliacao | Aprovado | 13 |
| 15 | AvaliacaoServiceTests | Consultar_ArredondaMediaDeCincoQuatroQuatroParaQuatroVirgulaTres | Aprovado | 1 |
| 16 | AvaliacaoServiceTests | Consultar_CalculaMediaComUmaCasaDecimal | Aprovado | 1 |
| 17 | AvaliacaoServiceTests | Consultar_NaoExpoeIdentidadeDoAvaliador | Aprovado | 22 |
| 18 | AvaliacaoServiceTests | Consultar_RefleteAlteracaoNaMedia | Aprovado | 10 |
| 19 | AvaliacaoServiceTests | Consultar_RejeitaUtilizadorInexistente | Aprovado | 1 |
| 20 | AvaliacaoServiceTests | Consultar_SemAvaliacoes_DevolveMediaNulaEListaVazia | Aprovado | 1 |
| 21 | AvaliacaoServiceTests | Criar_AceitaTodasAsClassificacoesValidas(valor: 1) | Aprovado | 0 |
| 22 | AvaliacaoServiceTests | Criar_AceitaTodasAsClassificacoesValidas(valor: 2) | Aprovado | 2 |
| 23 | AvaliacaoServiceTests | Criar_AceitaTodasAsClassificacoesValidas(valor: 3) | Aprovado | 0 |
| 24 | AvaliacaoServiceTests | Criar_AceitaTodasAsClassificacoesValidas(valor: 4) | Aprovado | 0 |
| 25 | AvaliacaoServiceTests | Criar_AceitaTodasAsClassificacoesValidas(valor: 5) | Aprovado | 0 |
| 26 | AvaliacaoServiceTests | Criar_AceitaViagemConcluida | Aprovado | 1 |
| 27 | AvaliacaoServiceTests | Criar_GravacaoRecusadaPelaBd_Conflito | Aprovado | 4 |
| 28 | AvaliacaoServiceTests | Criar_ImpedeAutoavaliacao | Aprovado | 9 |
| 29 | AvaliacaoServiceTests | Criar_PermiteAvaliacaoEntreDoisPassageirosAceites | Aprovado | 1 |
| 30 | AvaliacaoServiceTests | Criar_PermiteAvaliacaoNoSentidoInverso | Aprovado | 1 |
| 31 | AvaliacaoServiceTests | Criar_PermitePassageiroAvaliarCondutor | Aprovado | 1 |
| 32 | AvaliacaoServiceTests | Criar_RejeitaAvaliadorSemParticipacaoAceite | Aprovado | 1 |
| 33 | AvaliacaoServiceTests | Criar_RejeitaAvaliadoSemParticipacaoAceite | Aprovado | 3 |
| 34 | AvaliacaoServiceTests | Criar_RejeitaClassificacaoInvalida(valor: 0) | Aprovado | 2 |
| 35 | AvaliacaoServiceTests | Criar_RejeitaClassificacaoInvalida(valor: -1) | Aprovado | 0 |
| 36 | AvaliacaoServiceTests | Criar_RejeitaClassificacaoInvalida(valor: 100) | Aprovado | 0 |
| 37 | AvaliacaoServiceTests | Criar_RejeitaClassificacaoInvalida(valor: 6) | Aprovado | 1 |
| 38 | AvaliacaoServiceTests | Criar_RejeitaDuplicadoNaMesmaViagem | Aprovado | 1 |
| 39 | AvaliacaoServiceTests | Criar_RejeitaMesmoParNoutraViagem | Aprovado | 6 |
| 40 | AvaliacaoServiceTests | Criar_RejeitaUtilizadorInexistente | Aprovado | 2 |
| 41 | AvaliacaoServiceTests | Criar_RejeitaViagemInexistente | Aprovado | 3 |
| 42 | AvaliacaoServiceTests | Criar_RejeitaViagemNaoConcluida(estado: Ativa) | Aprovado | 1 |
| 43 | AvaliacaoServiceTests | Criar_RejeitaViagemNaoConcluida(estado: Indisponibilizada) | Aprovado | 0 |
| 44 | CalculadoraDeCustoAdditionalTests | ArredondaMetadeParaLongeDeZero | Aprovado | 0 |
| 45 | CalculadoraDeCustoAdditionalTests | CalculaValorPorOcupante(distancia: 0, preco: 0.10000000000000001, passageiros: 0, esperado: 0) | Aprovado | 0 |
| 46 | CalculadoraDeCustoAdditionalTests | CalculaValorPorOcupante(distancia: 10, preco: 0, passageiros: 2, esperado: 0) | Aprovado | 0 |
| 47 | CalculadoraDeCustoAdditionalTests | CalculaValorPorOcupante(distancia: 10, preco: 0.20000000000000001, passageiros: 0, esperado: 2) | Aprovado | 0 |
| 48 | CalculadoraDeCustoAdditionalTests | CalculaValorPorOcupante(distancia: 10, preco: 0.20000000000000001, passageiros: 1, esperado: 1) | Aprovado | 1 |
| 49 | CalculadoraDeCustoAdditionalTests | CalculaValorPorOcupante(distancia: 30, preco: 0.10000000000000001, passageiros: 2, esperado: 1) | Aprovado | 2 |
| 50 | CalculadoraDeCustoAdditionalTests | RejeitaArgumentosNegativos(distancia: 1, preco: -1, passageiros: 0) | Aprovado | 4 |
| 51 | CalculadoraDeCustoAdditionalTests | RejeitaArgumentosNegativos(distancia: -1, preco: 1, passageiros: 0) | Aprovado | 0 |
| 52 | CalculadoraDeCustoAdditionalTests | RejeitaArgumentosNegativos(distancia: 1, preco: 1, passageiros: -1) | Aprovado | 0 |
| 53 | CalculadoraDeCustoTests | DividePeloCondutorEMaisDoisPassageiros | Aprovado | 0 |
| 54 | CalculadoraDeCustoTests | RejeitaDistanciaNegativa | Aprovado | 0 |
| 55 | DispositivosControllerTests | Registar_ComClaimSub_UtilizaIdDoUtilizador | Aprovado | 1 |
| 56 | DispositivosControllerTests | Registar_ComTokenDemasiadoLongo_Devolve400 | Aprovado | 1 |
| 57 | DispositivosControllerTests | Registar_ComTokenValido_RegistaEDevolve204 | Aprovado | 4 |
| 58 | DispositivosControllerTests | Registar_ComTokenVazio_Devolve400(token: "   ") | Aprovado | 0 |
| 59 | DispositivosControllerTests | Registar_ComTokenVazio_Devolve400(token: "") | Aprovado | 1 |
| 60 | DispositivosControllerTests | Registar_SemIdentificadorValido_Devolve401 | Aprovado | 1 |
| 61 | DispositivosControllerTests | Remover_ComTokenValido_DesativaEDevolve204 | Aprovado | 1 |
| 62 | DispositivosControllerTests | Remover_ComTokenVazio_Devolve400 | Aprovado | 4 |
| 63 | DispositivosControllerTests | Remover_SemIdentificadorValido_Devolve401 | Aprovado | 1 |
| 64 | EntidadesTests | Avaliacao_Alterar_MudaSoOValor | Aprovado | 0 |
| 65 | EntidadesTests | Avaliacao_ValorForaDeUmACinco_ERecusado | Aprovado | 0 |
| 66 | EntidadesTests | Avaliacao_ValorValido_DeUmACinco(classificacao: 0, esperado: False) | Aprovado | 0 |
| 67 | EntidadesTests | Avaliacao_ValorValido_DeUmACinco(classificacao: 1, esperado: True) | Aprovado | 0 |
| 68 | EntidadesTests | Avaliacao_ValorValido_DeUmACinco(classificacao: 5, esperado: True) | Aprovado | 0 |
| 69 | EntidadesTests | Avaliacao_ValorValido_DeUmACinco(classificacao: 6, esperado: False) | Aprovado | 0 |
| 70 | EntidadesTests | ConfirmacaoConta_DentroDaValidade_PodeSerUsada | Aprovado | 0 |
| 71 | EntidadesTests | ConfirmacaoConta_Expirada_ERecusada | Aprovado | 0 |
| 72 | EntidadesTests | ConfirmacaoConta_SoServeUmaVez | Aprovado | 0 |
| 73 | EntidadesTests | Notificacao_MarcarComoLida_MantemAPrimeiraLeitura | Aprovado | 0 |
| 74 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: AlteracaoViagem, esperado: False) | Aprovado | 0 |
| 75 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: Aviso, esperado: False) | Aprovado | 0 |
| 76 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: NovaMensagem, esperado: False) | Aprovado | 0 |
| 77 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: PedidoAceite, esperado: True) | Aprovado | 0 |
| 78 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: PedidoRecebido, esperado: True) | Aprovado | 0 |
| 79 | EntidadesTests | Notificacao_OriginaPush_SoNosTresTiposDePedido(tipo: PedidoRejeitado, esperado: True) | Aprovado | 0 |
| 80 | EntidadesTests | Utilizador_AtualizarPerfil_MudaOsTresCampos | Aprovado | 0 |
| 81 | EntidadesTests | Utilizador_Confirmar_FicaConfirmado | Aprovado | 0 |
| 82 | EntidadesTests | Utilizador_Registar_FicaPorConfirmar | Aprovado | 0 |
| 83 | GlobalExceptionHandlerCoverageTests | TrataExcecaoComEstadoEDetalhesCorretos(negocio: False, escreveu: False) | Aprovado | 0 |
| 84 | GlobalExceptionHandlerCoverageTests | TrataExcecaoComEstadoEDetalhesCorretos(negocio: False, escreveu: True) | Aprovado | 3 |
| 85 | GlobalExceptionHandlerCoverageTests | TrataExcecaoComEstadoEDetalhesCorretos(negocio: True, escreveu: False) | Aprovado | 1 |
| 86 | GlobalExceptionHandlerCoverageTests | TrataExcecaoComEstadoEDetalhesCorretos(negocio: True, escreveu: True) | Aprovado | 0 |
| 87 | HistoricoControllerTests | Consultar_SemIdentificadorValido_Devolve401 | Aprovado | 1 |
| 88 | HistoricoControllerTests | Consultar_UtilizaIdDoUtilizadorAutenticado | Aprovado | 4 |
| 89 | I051ValidacoesControllersTests | F001_AtualizacaoPerfilSemIdentidade_Devolve401 | Aprovado | 1 |
| 90 | I051ValidacoesControllersTests | F001_AtualizacaoPerfilValida_AtualizaDadosDoUtilizador | Aprovado | 33 |
| 91 | I051ValidacoesControllersTests | F001_ConfirmacaoSemToken_Devolve400 | Aprovado | 1 |
| 92 | I051ValidacoesControllersTests | F001_ConsultaPerfilSemIdentidade_Devolve401 | Aprovado | 1 |
| 93 | I051ValidacoesControllersTests | F001_LoginSemCredenciais_Devolve401(email: "", password: "senha") | Aprovado | 1 |
| 94 | I051ValidacoesControllersTests | F001_LoginSemCredenciais_Devolve401(email: "aluno@estg.ipp.pt", password: "") | Aprovado | 0 |
| 95 | I051ValidacoesControllersTests | F001_LoginValido_DevolveTokenDeAcesso | Aprovado | 298 |
| 96 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "", email: "aluno@estg.ipp.pt", password: "password123456") | Aprovado | 0 |
| 97 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "", password: "password123456") | Aprovado | 0 |
| 98 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "aluno@estg.ipp.pt", password: "1234567890") | Aprovado | 0 |
| 99 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "aluno@estg.ipp.pt", password: "abc1234") | Aprovado | 0 |
| 100 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "aluno@estg.ipp.pt", password: "abcdefghij") | Aprovado | 2 |
| 101 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "aluno@estg.ipp.pt", password: "curta") | Aprovado | 0 |
| 102 | I051ValidacoesControllersTests | F001_RegistoInvalido_Devolve400(nome: "Aluno", email: "aluno@gmail.com", password: "password123456") | Aprovado | 0 |
| 103 | I051ValidacoesControllersTests | F001_RegistoValido_CriaContaPendenteEEnviaConfirmacao | Aprovado | 172 |
| 104 | I051ValidacoesControllersTests | F002_CriarViagemSemIdentidade_Devolve401 | Aprovado | 1 |
| 105 | I051ValidacoesControllersTests | F002_CriarViagemValida_CriaViagemComDadosDaRota | Aprovado | 34 |
| 106 | I051ValidacoesControllersTests | F002_EditarViagemValida_AtualizaDataELugares | Aprovado | 53 |
| 107 | I051ValidacoesControllersTests | F002_IndisponibilizarViagemValida_AlteraEstado | Aprovado | 29 |
| 108 | I051ValidacoesControllersTests | F003_FiltroDataEHora_DevolveApenasPartidasCorrespondentes | Aprovado | 45 |
| 109 | I051ValidacoesControllersTests | F003_PaginacaoValida_DevolveSegundaPaginaSemDuplicacoes | Aprovado | 40 |
| 110 | I051ValidacoesControllersTests | F003_PesquisaComPaginacaoInvalida_Devolve400(pagina: 0, tamanho: 20) | Aprovado | 1 |
| 111 | I051ValidacoesControllersTests | F003_PesquisaComPaginacaoInvalida_Devolve400(pagina: 1, tamanho: 0) | Aprovado | 0 |
| 112 | I051ValidacoesControllersTests | F003_PesquisaComPaginacaoInvalida_Devolve400(pagina: 1, tamanho: 101) | Aprovado | 0 |
| 113 | I051ValidacoesControllersTests | F003_PesquisaComPaginacaoInvalida_Devolve400(pagina: -1, tamanho: 20) | Aprovado | 0 |
| 114 | I051ValidacoesControllersTests | F003_PesquisaSemFiltros_DevolveApenasViagensElegiveis | Aprovado | 29 |
| 115 | I051ValidacoesControllersTests | F003_PesquisaValida_FiltraOrdenaEPaginaResultados | Aprovado | 50 |
| 116 | I051ValidacoesControllersTests | UT006_RegistoGeraTokenValidoEEnviaEmail | Aprovado | 161 |
| 117 | I051ValidacoesControllersTests | UT007_TokenValidoConfirmaContaEImpedeReutilizacao | Aprovado | 203 |
| 118 | I051ValidacoesControllersTests | UT008_TokenDesconhecidoERejeitado | Aprovado | 215 |
| 119 | I051ValidacoesControllersTests | UT008_TokenExpiradoERejeitado | Aprovado | 159 |
| 120 | I051ValidacoesControllersTests | UT008_TokenVazioERejeitado | Aprovado | 190 |
| 121 | I052ParticipacaoServiceTests | Aceitar_CapacidadeInconsistente_NaoConfirma | Aprovado | 1 |
| 122 | I052ParticipacaoServiceTests | Aceitar_OutroUtilizador_NaoAutorizado | Aprovado | 1 |
| 123 | I052ParticipacaoServiceTests | Aceitar_PedidoInexistente_NaoEncontrada | Aprovado | 4 |
| 124 | I052ParticipacaoServiceTests | Aceitar_PedidoValido_DescontaLugarENotificaPassageiro | Aprovado | 5 |
| 125 | I052ParticipacaoServiceTests | Aceitar_SemLugares_NaoAlteraEstado | Aprovado | 1 |
| 126 | I052ParticipacaoServiceTests | Cancelar_PendentePeloPassageiro_NaoAlteraCapacidade | Aprovado | 2 |
| 127 | I052ParticipacaoServiceTests | Cancelar_PorTerceiro_NaoAutorizado | Aprovado | 1 |
| 128 | I052ParticipacaoServiceTests | Rejeitar_PedidoJaAceite_Conflito | Aprovado | 1 |
| 129 | I052ParticipacaoServiceTests | Rejeitar_PedidoValido_NotificaSemOcuparLugar | Aprovado | 2 |
| 130 | NotificadorPedidosSignalRTests | PedidoAceiteAsync_DeveEnviarEventoAoDestinatario | Aprovado | 1 |
| 131 | NotificadorPedidosSignalRTests | PedidoRecebidoAsync_DeveEnviarEventoAoDestinatario | Aprovado | 1 |
| 132 | NotificadorPedidosSignalRTests | PedidoRecebidoAsync_QuandoEnvioFalha_NaoDevePropagarExcecao | Aprovado | 3 |
| 133 | NotificadorPedidosSignalRTests | PedidoRejeitadoAsync_DeveEnviarEventoAoDestinatario | Aprovado | 3 |
| 134 | OrdenadorDePontosTests | DestinoDoPassageiroVemDepoisDaRecolha_MesmoSendoMaisLento | Aprovado | 1 |
| 135 | OrdenadorDePontosTests | EscolheAMenorDuracaoTotal | Aprovado | 1 |
| 136 | OrdenadorDePontosTests | Exatamente16PontosAdicionais_EAceite | Aprovado | 2 |
| 137 | OrdenadorDePontosTests | MaisDe16PontosAdicionais_ERecusado | Aprovado | 1 |
| 138 | OrdenadorDePontosTests | SemNenhumaSequenciaViavel_DevolveNull | Aprovado | 1 |
| 139 | OrdenadorDePontosTests | SemPontosAdicionais_DevolveOrigemEDestino | Aprovado | 18 |
| 140 | ParticipacaoTests | Aceitar_RegistaOMomento | Aprovado | 5 |
| 141 | ParticipacaoTests | Cancelamento_MantemAceiteEm_ParaOHistorico | Aprovado | 0 |
| 142 | ParticipacaoTests | CancelarPorConclusao_SoPendentes | Aprovado | 0 |
| 143 | ParticipacaoTests | CancelarPorIndisponibilidade_JaTerminada_ERecusado(estado: Cancelada) | Aprovado | 3 |
| 144 | ParticipacaoTests | CancelarPorIndisponibilidade_JaTerminada_ERecusado(estado: Rejeitada) | Aprovado | 0 |
| 145 | ParticipacaoTests | CancelarPorIndisponibilidade_PendentesEAceites(estado: Aceite) | Aprovado | 0 |
| 146 | ParticipacaoTests | CancelarPorIndisponibilidade_PendentesEAceites(estado: Pendente) | Aprovado | 0 |
| 147 | ParticipacaoTests | NovaParticipacao_FicaPendente | Aprovado | 0 |
| 148 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Aceite, horasAtePartida: -1, permitido: False) | Aprovado | 0 |
| 149 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Aceite, horasAtePartida: 1, permitido: True) | Aprovado | 0 |
| 150 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Cancelada, horasAtePartida: 1, permitido: False) | Aprovado | 7 |
| 151 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Pendente, horasAtePartida: 1, permitido: True) | Aprovado | 0 |
| 152 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Pendente, horasAtePartida: -1, permitido: True) | Aprovado | 0 |
| 153 | ParticipacaoTests | PassageiroCancela_PendenteSempreEAceiteAntesDaPartida(estado: Rejeitada, horasAtePartida: 1, permitido: False) | Aprovado | 0 |
| 154 | ParticipacaoTests | Rejeitar_PassaARejeitada | Aprovado | 0 |
| 155 | ParticipacaoTests | SoPendente_PodeSerDecidido(estado: Aceite) | Aprovado | 0 |
| 156 | ParticipacaoTests | SoPendente_PodeSerDecidido(estado: Cancelada) | Aprovado | 1 |
| 157 | ParticipacaoTests | SoPendente_PodeSerDecidido(estado: Rejeitada) | Aprovado | 0 |
| 158 | PedidosUserIdProviderTests | GetUserId_DeveDevolverIdQuandoExisteNameIdentifierValido | Aprovado | 3 |
| 159 | PedidosUserIdProviderTests | GetUserId_DeveDevolverNullQuandoIdEInvalido | Aprovado | 0 |
| 160 | PedidosUserIdProviderTests | GetUserId_DeveUsarSubQuandoNameIdentifierNaoExiste | Aprovado | 0 |
| 161 | RegrasDeConsultaTests | Condutor_VeSempreASuaViagem | Aprovado | 0 |
| 162 | RegrasDeConsultaTests | Media_TemUmaCasaDecimal_ArredondadaParaCima | Aprovado | 1 |
| 163 | RegrasDeConsultaTests | OutroUtilizador_SoVeViagemPesquisavel | Aprovado | 3 |
| 164 | RegrasDeConsultaTests | SemAvaliacoes_NaoHaMedia | Aprovado | 0 |
| 165 | RegrasDeConsultaTests | ViagemAtivaFuturaComLugares_EPesquisavel | Aprovado | 0 |
| 166 | RegrasDeConsultaTests | ViagemTerminadaCheiaOuJaPartida_NaoEPesquisavel(estado: Ativa, lugares: 0, horas: 1) | Aprovado | 0 |
| 167 | RegrasDeConsultaTests | ViagemTerminadaCheiaOuJaPartida_NaoEPesquisavel(estado: Ativa, lugares: 2, horas: -1) | Aprovado | 0 |
| 168 | RegrasDeConsultaTests | ViagemTerminadaCheiaOuJaPartida_NaoEPesquisavel(estado: Concluida, lugares: 2, horas: 1) | Aprovado | 0 |
| 169 | RotaServiceTests | DestinoSemRecolha_NaoTemPrecedencia | Aprovado | 1 |
| 170 | RotaServiceTests | EscolheMenorDuracaoTotal_NaoOVizinhoMaisProximo | Aprovado | 12 |
| 171 | RotaServiceTests | MaisDe16PontosAdicionais_ERecusado | Aprovado | 2 |
| 172 | RotaServiceTests | Obter_ConflitoAoGravar_DevolveARota | Aprovado | 3 |
| 173 | RotaServiceTests | Obter_RotaGuardada_NaoChamaOServicoNemGrava | Aprovado | 2 |
| 174 | RotaServiceTests | Obter_RotaPorRecalcular_CalculaEGrava | Aprovado | 1 |
| 175 | RotaServiceTests | RecolhaAntesDoDestinoDoPassageiro | Aprovado | 2 |
| 176 | RotaServiceTests | RotaGuardada_ConservaARotaEContaOsPontosAdicionais | Aprovado | 4 |
| 177 | RotaServiceTests | SemPontosAdicionais_DevolveOrigemEDestino | Aprovado | 13 |
| 178 | ServicoEmailBrevoCoverageTests | EnviaPedidoBrevoComTokenEscapado(url: "https://app.test/confirmar", sufixo: "?token=a%20b") | Aprovado | 1 |
| 179 | ServicoEmailBrevoCoverageTests | EnviaPedidoBrevoComTokenEscapado(url: "https://app.test/confirmar?lang=pt", sufixo: "&token=a%20b") | Aprovado | 14 |
| 180 | ServicoEmailBrevoCoverageTests | RespostaNaoBemSucedida_PropagaErroHttp | Aprovado | 1 |
| 181 | ServicoEmailBrevoCoverageTests | SemConfiguracaoObrigatoria_NaoEnvia(chave: " ", remetente: "remetente@test.pt", url: "https://app.test/confirmar") | Aprovado | 0 |
| 182 | ServicoEmailBrevoCoverageTests | SemConfiguracaoObrigatoria_NaoEnvia(chave: "segredo", remetente: "remetente@test.pt", url: null) | Aprovado | 3 |
| 183 | ServicoEmailBrevoCoverageTests | SemConfiguracaoObrigatoria_NaoEnvia(chave: "segredo", remetente: null, url: "https://app.test/confirmar") | Aprovado | 0 |
| 184 | ServicoEmailBrevoCoverageTests | SemConfiguracaoObrigatoria_NaoEnvia(chave: null, remetente: "remetente@test.pt", url: "https://app.test/confirmar") | Aprovado | 0 |
| 185 | ServicoEmailBrevoCoverageTests | UsaNomeDeRemetenteConfigurado | Aprovado | 1 |
| 186 | ServicoMapasRotasMatrizTests | MatrizComUmPontoRejeitada | Aprovado | 2 |
| 187 | ServicoMapasRotasMatrizTests | MatrizPreservaValoresNulosEEnviaMetricas | Aprovado | 6 |
| 188 | ServicoMapasRotasMatrizTests | MatrizPropagaErroHttp | Aprovado | 2 |
| 189 | ServicoMapasRotasMatrizTests | MatrizSemChaveRejeitada | Aprovado | 1 |
| 190 | ServicoMapasRotasTests | CalcularCom404SemCodigoDeRota_ContaComoFalhaDoServico(corpo: "{\"error\":\"Not found\"}") | Aprovado | 1 |
| 191 | ServicoMapasRotasTests | CalcularCom404SemCodigoDeRota_ContaComoFalhaDoServico(corpo: "{\"error\":{\"code\":2099}}") | Aprovado | 0 |
| 192 | ServicoMapasRotasTests | CalcularCom404SemCodigoDeRota_ContaComoFalhaDoServico(corpo: "<html>Not found</html>") | Aprovado | 0 |
| 193 | ServicoMapasRotasTests | CalcularComRespostaMalformada_ContaComoFalhaDoServico(corpo: "[]") | Aprovado | 1 |
| 194 | ServicoMapasRotasTests | CalcularComRespostaMalformada_ContaComoFalhaDoServico(corpo: "{\"features\":[{}]}") | Aprovado | 0 |
| 195 | ServicoMapasRotasTests | CalcularComRespostaMalformada_ContaComoFalhaDoServico(corpo: "{}") | Aprovado | 1 |
| 196 | ServicoMapasRotasTests | CalcularComRespostaMalformada_ContaComoFalhaDoServico(corpo: "<html>Bad gateway</html>") | Aprovado | 0 |
| 197 | ServicoMapasRotasTests | CalcularConverteMetrosEmQuilometrosEArredondaMinutos | Aprovado | 1 |
| 198 | ServicoMapasRotasTests | CalcularErroHttpPropagaExcecao | Aprovado | 2 |
| 199 | ServicoMapasRotasTests | CalcularSemCaminhoEntreOsPontos_RetornaNull(codigo: 2009) | Aprovado | 0 |
| 200 | ServicoMapasRotasTests | CalcularSemCaminhoEntreOsPontos_RetornaNull(codigo: 2010) | Aprovado | 1 |
| 201 | ServicoMapasRotasTests | CalcularSemChaveFalha | Aprovado | 1 |
| 202 | ServicoMapasRotasTests | CalcularSemRotasRetornaNull | Aprovado | 1 |
| 203 | ServicoMapasRotasTests | ConfiguradoRefletePresencaDaChave | Aprovado | 0 |
| 204 | ServicoMapasRotasTests | GeocodificarComRespostaMalformada_ContaComoFalhaDoServico(corpo: "{\"features\":[{\"geometry\":{\"coordinates\":[1]}"···) | Aprovado | 0 |
| 205 | ServicoMapasRotasTests | GeocodificarComRespostaMalformada_ContaComoFalhaDoServico(corpo: "{}") | Aprovado | 1 |
| 206 | ServicoMapasRotasTests | GeocodificarComRespostaMalformada_ContaComoFalhaDoServico(corpo: "<html>Bad gateway</html>") | Aprovado | 2 |
| 207 | ServicoMapasRotasTests | GeocodificarDevolveLatitudeLongitudeEEnviaPesquisaEscapada | Aprovado | 4 |
| 208 | ServicoMapasRotasTests | GeocodificarErroHttpPropagaExcecao | Aprovado | 1 |
| 209 | ServicoMapasRotasTests | GeocodificarSemChaveFalha | Aprovado | 1 |
| 210 | ServicoMapasRotasTests | GeocodificarSemResultadosRetornaNull | Aprovado | 1 |
| 211 | ServicoMapasRotasTests | MatrizComRespostaMalformada_ContaComoFalhaDoServico | Aprovado | 2 |
| 212 | ServicoPushTests | EnviarAsync_Com404SemTokenInvalido_MantemODispositivo | Aprovado | 144 |
| 213 | ServicoPushTests | EnviarAsync_ComErro500_NaoDesativaToken | Aprovado | 196 |
| 214 | ServicoPushTests | EnviarAsync_ComErroNumPrimeiroDispositivo_ContinuaParaOSeguinte | Aprovado | 240 |
| 215 | ServicoPushTests | EnviarAsync_ComErroUnregistered_DesativaToken | Aprovado | 314 |
| 216 | ServicoPushTests | EnviarAsync_ComFirebaseNaoConfigurado_NaoEnviaPush | Aprovado | 1 |
| 217 | ServicoPushTests | EnviarAsync_ComSucesso_EnviaMensagemFcm | Aprovado | 546 |
| 218 | ServicoPushTests | EnviarAsync_ComTokenNaoRegistado_DesativaTokenInvalido | Aprovado | 649 |
| 219 | ServicoPushTests | EnviarAsync_SemDispositivosAtivos_NaoFazPedidosHttp | Aprovado | 292 |
| 220 | TempoLimiteClientesHttpTests | I110_153_ClienteDoEmail_TemTempoLimiteCurto | Aprovado | 5 |
| 221 | ValidadorEmailAcademicoTests | I110_60_SoAceitaOsDominiosDaLista(email: "aluno@ess.ipp.pt", esperado: True) | Aprovado | 0 |
| 222 | ValidadorEmailAcademicoTests | I110_60_SoAceitaOsDominiosDaLista(email: "ALUNO@ESTG.IPP.PT", esperado: True) | Aprovado | 0 |
| 223 | ValidadorEmailAcademicoTests | I110_60_SoAceitaOsDominiosDaLista(email: "aluno@estg.ipp.pt", esperado: True) | Aprovado | 0 |
| 224 | ValidadorEmailAcademicoTests | I110_60_SoAceitaOsDominiosDaLista(email: "aluno@gmail.com", esperado: False) | Aprovado | 0 |
| 225 | ValidadorEmailAcademicoTests | I110_60_SoAceitaOsDominiosDaLista(email: "aluno@xestg.ipp.pt", esperado: False) | Aprovado | 0 |
| 226 | ValidadorEmailAcademicoTests | I110_61_DominioComArroba_TemOMesmoEfeito | Aprovado | 0 |
| 227 | ValidadorEmailAcademicoTests | I110_62_SemDominiosConfigurados_RecusaTudo | Aprovado | 1 |
| 228 | ValidadorEmailFormatoTests | Aceita(email: "8220000@ESTG.IPP.PT") | Aprovado | 0 |
| 229 | ValidadorEmailFormatoTests | Aceita(email: "aluno@estg.ipp.pt") | Aprovado | 0 |
| 230 | ValidadorEmailFormatoTests | Aceita(email: "nome.apelido+teste@estg.ipp.pt") | Aprovado | 0 |
| 231 | ValidadorEmailFormatoTests | Recusa(email: " aluno@estg.ipp.pt") | Aprovado | 0 |
| 232 | ValidadorEmailFormatoTests | Recusa(email: "@estg.ipp.pt") | Aprovado | 0 |
| 233 | ValidadorEmailFormatoTests | Recusa(email: "a b@estg.ipp.pt") | Aprovado | 0 |
| 234 | ValidadorEmailFormatoTests | Recusa(email: "aluno@gmail.com") | Aprovado | 0 |
| 235 | ValidadorEmailFormatoTests | Recusa(email: "x@gmail.com@estg.ipp.pt") | Aprovado | 0 |
| 236 | ViagemTests | Alterar_LugaresAbaixoDosOcupados_ERecusado | Aprovado | 0 |
| 237 | ViagemTests | Alterar_MantemOcupadosERecalculaDisponiveis | Aprovado | 0 |
| 238 | ViagemTests | Alterar_ViagemNaoAtivaOuJaPartida_ERecusado(estado: Ativa, horasAtePartida: -1) | Aprovado | 0 |
| 239 | ViagemTests | Alterar_ViagemNaoAtivaOuJaPartida_ERecusado(estado: Concluida, horasAtePartida: 1) | Aprovado | 0 |
| 240 | ViagemTests | Alterar_ViagemNaoAtivaOuJaPartida_ERecusado(estado: Indisponibilizada, horasAtePartida: 1) | Aprovado | 0 |
| 241 | ViagemTests | AtualizarRota_GuardaARotaEADistanciaMasNaoADuracao | Aprovado | 0 |
| 242 | ViagemTests | Concluir_AntesDaPartidaOuNaoAtiva_ERecusado(estado: Ativa, horasAtePartida: 1) | Aprovado | 1 |
| 243 | ViagemTests | Concluir_AntesDaPartidaOuNaoAtiva_ERecusado(estado: Concluida, horasAtePartida: -1) | Aprovado | 0 |
| 244 | ViagemTests | Concluir_AntesDaPartidaOuNaoAtiva_ERecusado(estado: Indisponibilizada, horasAtePartida: -1) | Aprovado | 0 |
| 245 | ViagemTests | Concluir_DepoisDaPartida_FicaConcluida | Aprovado | 0 |
| 246 | ViagemTests | Criar_FicaAtivaComTodosOsLugaresLivres | Aprovado | 0 |
| 247 | ViagemTests | DeveConcluir_SemDuracao_ConcluiAHoraDePartida | Aprovado | 0 |
| 248 | ViagemTests | DeveConcluir_SoDepoisDaPartidaMaisADuracao(minutosAtePartida: -59, duracao: 60, esperado: False) | Aprovado | 0 |
| 249 | ViagemTests | DeveConcluir_SoDepoisDaPartidaMaisADuracao(minutosAtePartida: -60, duracao: 60, esperado: True) | Aprovado | 0 |
| 250 | ViagemTests | DeveConcluir_ViagemNaoAtiva_Falso | Aprovado | 0 |
| 251 | ViagemTests | Indisponibilizar_LibertaTodosOsLugares | Aprovado | 0 |
| 252 | ViagemTests | Indisponibilizar_ViagemNaoAtiva_ERecusado(estado: Concluida) | Aprovado | 0 |
| 253 | ViagemTests | Indisponibilizar_ViagemNaoAtiva_ERecusado(estado: Indisponibilizada) | Aprovado | 0 |
| 254 | ViagemTests | LibertarLugar_ComTodosLivres_ERecusado | Aprovado | 0 |
| 255 | ViagemTests | LugaresConsistentes_ComparaComOsAceites(oferecidos: 1, disponiveis: -1, aceites: 2, esperado: False) | Aprovado | 0 |
| 256 | ViagemTests | LugaresConsistentes_ComparaComOsAceites(oferecidos: 3, disponiveis: 2, aceites: 1, esperado: True) | Aprovado | 0 |
| 257 | ViagemTests | LugaresConsistentes_ComparaComOsAceites(oferecidos: 3, disponiveis: 3, aceites: 1, esperado: False) | Aprovado | 0 |
| 258 | ViagemTests | MarcarRotaPorRecalcular_LimpaARotaEMantemADistancia | Aprovado | 0 |
| 259 | ViagemTests | OcuparELibertarLugar_AcompanhamOsDisponiveis | Aprovado | 0 |
| 260 | ViagemTests | OcuparLugar_SemLugares_ERecusado | Aprovado | 0 |


### Conclusão da atualização

Os 260 testes unitários da versão entregue foram aprovados, sem falhas e sem saltos.
