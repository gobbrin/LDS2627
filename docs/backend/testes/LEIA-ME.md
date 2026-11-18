# CampusCarpool — Aumentar a cobertura de testes

> **Plano de testes em vigor (m4):** [m4-plano-testes-backend-v01.md](m4-plano-testes-backend-v01.md) (I112, 17/11/2026): modo duplo SQLite e SQL Server, contrato de erro `{ erro }`, testes de arquitetura, regras para escrever testes e lacunas registadas. O plano de m3 (`m3-s03-i031-plano-testes-backend.docx`, I031) fica como está. O `.docx` do plano de m4 gera-se do `.md` com `pandoc m4-plano-testes-backend-v01.md -o m4-plano-testes-backend-v01.docx` (modelo por omissão do pandoc; há um pandoc no pacote Python `pypandoc_binary`). O texto depois da secção do Postman é o registo de m3 sobre a cobertura.

## Postman (I112, R-Q06)

**Em vigor:** `postman/CampusCarpool.postman_collection.json` com o ambiente `postman/CampusCarpool.postman_environment.json` (17/11/2026; 18/11: bilhete do hub). Cobre os 46 endpoints de `/api/v1` mais `GET /` e `GET /health` (o hub SignalR `/hubs/pedidos` fica de fora: não é REST, está nos testes de integração; a coleção cobre o `POST /autenticacao/bilhete-hub` que dá acesso a ele; lista em `docs/backend/arquitetura/GUIA-DO-CODIGO.md` §8), em 92 pedidos e 13 pastas, na ordem de um fluxo que funciona: registo e login → bilhete do hub → perfil → mapas → viagens (criar, simular a alteração, alterar) → pesquisa → simular o pedido e pedir → simular (condutor), aceitar, rejeitar, cancelar, indisponibilizar → avisos e mensagens (conversa por cursor) → notificações e dispositivos → concluir → avaliar → histórico → fim de sessão.

Cada pedido tem a **resposta esperada** na descrição e como testes `pm.test`: o código HTTP, os campos principais e, nos erros, o corpo `{ erro }` só com esse campo e a mensagem exata. O 401 do JwtBearer não tem corpo. As variáveis encadeiam-se (tokens, ids, cursor da conversa); cada corrida usa emails novos (`sufixo`).

Como correr (Postman: importar os dois ficheiros e usar o *Collection Runner*; ou `newman run CampusCarpool.postman_collection.json -e CampusCarpool.postman_environment.json`):

1. API local (`backend/README.md`) com a base de dados migrada; `baseUrl` = `http://localhost:5100`. O domínio dos emails (`dominioEmail`) tem de estar em `EmailAcademico:Dominios` (`estg.ipp.pt` por omissão).
2. **Confirmação da conta:** o token só chega por email (Brevo). Copiar o token da ligação recebida para `tokenConfirmacaoCondutor` e `tokenConfirmacaoPassageiro`; sem eles os dois pedidos de confirmação são saltados e as contas têm de ser confirmadas por outro meio antes do «Login do condutor» (em desenvolvimento: `UPDATE Utilizadores SET EmailConfirmado = 1 WHERE Email = …`).
3. **Mapas:** com `Here:ApiKey` (ou `Maps:ApiKey`) os pedidos de mapas respondem 200/201 com `geometria` e `geometriaAproximada`. **Sem chave, a resposta esperada é 503 `{ "erro": "Serviço de mapas não configurado." }`** e os testes aceitam-na; mas então `POST /viagens` não cria a viagem e é preciso preencher `viagemId` e `viagemBId` com viagens gravadas à mão (ativas, do condutor, Porto → Braga; a A com a partida em `partidaFutura`).
4. A viagem A parte `segundosAtePartida` (180 por omissão) depois de criada: o pedido «Concluir a viagem A» espera por essa hora, porque só se conclui depois da partida.
5. A autenticação aceita 10 pedidos/min por IP: a coleção faz 9 (registo, reenvio e login). Não a correr duas vezes no mesmo minuto (429).

**Verificado a 17/11/2026** (sem `newman` instalado; um executor mínimo em Node correu os mesmos scripts `pm.test`): API local em SQL Server (Docker), sem chaves de mapas nem Brevo; contas confirmadas e as duas viagens gravadas na BD como nos passos 2 e 3. Resultado: 88 pedidos corridos (2 saltados: confirmação por token), 168 testes, 0 falhas.

**Verificado a 18/11/2026 com o Newman 6.2.3** (mesma preparação; gancho do Newman confirma as contas na BD antes do «Login do condutor» e grava as viagens quando `POST /viagens` não as cria): sem chave de mapas, 90 pedidos corridos de 92 (2 saltados: confirmação por token), 172 testes, **0 falhas**. Com a `Here:ApiKey` dos User Secrets a HERE respondeu 401 «apiKey invalid. apiKey not enabled.» a todas as chamadas (13: geocodificação e matriz) e os 8 pedidos de mapas deram 502 `{ erro }` (19 testes falhados, só nesses pedidos); o resto do fluxo passou.

**Verificado a 18/11/2026 com uma chave HERE válida** (`Here:ApiKey` nos User Secrets, sem `Maps:ApiKey` nem Brevo; as viagens criadas pela API, sem gravar na BD): 90 pedidos corridos de 92 (2 saltados: confirmação por token), 180 testes, **0 falhas**. Os 7 pedidos de mapas (`geocodificar`, `calcular`, criar a viagem A e B, `rotas/viagens`, `simular-alteracao`, `simulacao` e `simular-pedido`) responderam 200/201 com a linha real da HERE (`geometriaAproximada: false`; Porto → Braga 54,50 km, 44 min, 1068 pontos). A corrida fez 8 chamadas à HERE (3 geocodificações, 4 rotas, 1 matriz). **Por verificar:** a confirmação por token real (Brevo).

**Histórico (m3, não usar para validar a API atual):** `postman/CampusCarpool-API-Backend-Corrigida-Simulacao.postman_collection.json`, `postman/CampusCarpool-I034-I037.postman_collection.json` e as coleções de `I055/` descrevem o comportamento de m3 (por exemplo, 503 no registo quando o email falhava, erros em ProblemDetails, rotas sem paginação). Ficam como evidência de m3.

## Situação inicial (medição fornecida)
- Cobertura global: **24,96% das linhas** (1310/5247).
- **58,63% dos ramos** (533/909).
- A cobertura inclui milhares de linhas de *migrations* e código gerado.
- `CampusCarpool.Infrastructure` apresenta apenas 4,31% na medição original.

## O que foi acrescentado
- 8 testes para `MapasService.CalcularComParagensAsync` e `CalcularMatrizAsync`.
- Configuração `coverage.runsettings` para excluir exclusivamente ficheiros de migrations e `obj` gerados automaticamente.

A exclusão de código gerado é uma decisão de medição, **não representa testes novos nem melhoria efetiva da cobertura desse código**. A meta de 80% deve ser avaliada sobre o âmbito acordado pela equipa/docente. Se for exigida cobertura bruta, não uses exclusões.

## Aplicar
1. Copiar o ficheiro de testes para `tests/CampusCarpool.Tests/Unit/`.
2. Copiar `coverage.runsettings` para a raiz `backend/`.
3. Na raiz, executar:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --results-directory .\artifacts\cobertura-atualizada --logger "trx;LogFilePrefix=cobertura-atualizada"
```

4. Verificar o ficheiro `coverage.cobertura.xml` gerado. Exemplo:

```powershell
$xml = Get-ChildItem .\artifacts\cobertura-atualizada -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$c = Get-Content $xml.FullName
"Linhas: {0:P2}; Ramos: {1:P2}" -f [double]::Parse($c.coverage.'line-rate',[cultureinfo]::InvariantCulture), [double]::Parse($c.coverage.'branch-rate',[cultureinfo]::InvariantCulture)
```

## Limitações
Não foi possível executar `dotnet test` neste ambiente (SDK .NET não instalado). Os testes são propostos com base no código-fonte enviado e **não estão validados**. Não é possível garantir 80% sem nova execução. Para atingir 80% no código não gerado, provavelmente serão necessários testes adicionais de `Infrastructure` (repositórios, serviços e integrações), além de caminhos ainda não cobertos na API.
