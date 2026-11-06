# I034–I037 — Implementação e validação

## I034 — Perfil
- GET/PUT `/api/v1/utilizadores/me` exigem JWT. O ID vem do token, nunca do body.
- O DTO só aceita nome, curso e instituição. Email, confirmação e password não são alteráveis.

## I035 — Viagens
- POST/GET/PUT `/api/v1/viagens`, POST `/api/v1/viagens/{id}/indisponibilizar` e `/concluir`.
- Só o condutor pode editar ou mudar o estado. Criação exige conta confirmada, rota válida e partida UTC futura.
- A conclusão só é permitida após a partida. Pedidos pendentes são cancelados; aceites são preservados.

## I036 — Mapas
- `/api/v1/rotas/geocodificar` e `/calcular` dependem de `Maps:ApiKey` (OpenRouteService).
- Configurar chave nos User Secrets; falhas externas retornam 502/504, configuração ausente 503.

## I037 — Pesquisa
- GET `/api/v1/pesquisa/viagens` suporta origem, destino, data, hora, pagina e tamanho.
- Só mostra viagens ativas, futuras e com lugares. A pesquisa por hora considera hora UTC no estado atual.

## Testes no Postman
1. Confirmar conta, fazer login e guardar `token` na collection.
2. Importar `CampusCarpool-I034-I037.postman_collection.json`.
3. Executar perfil, rota, criação, consulta e pesquisa. Atualizar `partidaEm` para data UTC futura.
4. Testar sem token (401), token de outro utilizador (403 em mutações), dados inválidos (400), viagem inexistente (404), mapas não configurados (503).
5. Não executar indisponibilização antes dos testes de pesquisa; muda o estado da viagem.

## Limitações e pendências
- Não foi possível executar `dotnet build` ou testes neste ambiente (SDK ausente).
- Integração real com SQL Server, SMTP e OpenRouteService exige credenciais e serviços externos.
- Os filtros de pesquisa são textuais, não geoespaciais. Validar requisitos finais da M2/M3 antes de fechar as Issues.
- Rever o aviso NU1903 de Microsoft.OpenApi e atualizar para versão corrigida compatível.
- JWT foi retirado do appsettings.json: configurar `Jwt:Key` em User Secrets ou ambiente.
