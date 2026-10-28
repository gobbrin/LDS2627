# I055 — Collection Postman CampusCarpool

## Fonte
Controllers no `CampusCarpool(20261028-023501).zip`, duas collections fornecidas e environment fornecido. Não houve execução contra servidor.

## Importar
Importar `I055_CampusCarpool_Postman_Collection.json` e `I055_CampusCarpool_Postman_Environment.json`. Selecionar o ambiente, configurar `baseUrl`, `email`, `password` e fazer login. O login guarda `accessToken`. Para fluxos entre condutor e passageiro, obter tokens separadamente e preencher `condutorToken` e `passageiroToken`; preencher IDs reais.

## Ordem recomendada
1. Autenticação e perfil; 2. Rotas; 3. Criar viagem; 4. Pesquisa; 5. Criar pedido como passageiro; 6. Aceitar/rejeitar/cancelar com dados independentes; 7. Notificações, mensagens e avisos; 8. Testes negativos.

## Restrições
- Pastas `[MANUAL]` requerem pré-condições, dados reais, utilizadores e/ou alteram estado. Não executar a coleção inteira sem preparar dados.
- AvaliacoesController.cs e HistoricoController.cs têm 0 bytes no ZIP original. A coleção I048 foi preservada como referência, não representa endpoints confirmados.
- Os endpoints de dispositivos da I047 são condicionais: não existem no ZIP original e só funcionam após integração da I047.
- Serviços externos de mapas, SMTP e Firebase requerem configuração.
- A resposta esperada dos testes dependentes de dados pressupõe que os pré-requisitos se verificam; HTTP 4xx/5xx nesses casos deve ser investigado, não ignorado.
- Os testes Postman não foram executados neste ambiente.
- O ambiente tem variáveis vazias que devem ser preenchidas antes de executar testes dependentes de identidade.

## Contagem
91 pedidos/testes definidos na collection (incluindo 18 de I048 ainda não disponíveis).
