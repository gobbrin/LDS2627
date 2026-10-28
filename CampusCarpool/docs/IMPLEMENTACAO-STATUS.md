# Estado da implementação

- Entidades e DbContext EF Core: implementados como base inicial; requerem revisão dos modelos M2 aprovados.
- JWT Bearer: validação e emissão básica de token de 60 min implementadas. A chave tem de ser fornecida externamente.
- Registo: persistência e hash implementados, mas email de confirmação **não enviado** (resposta 501); requer serviço de email.
- Confirmação: endpoint disponível quando se obtiver um token por canal seguro.
- Viagens: consulta disponível; criação devolve 501 até integração de mapas e rotas, evitando dados inconsistentes.
- Pesquisa: consulta simples por data; filtros de proximidade e hora ainda não implementados.
- Migrations: **não geradas**; requerem SDK dotnet-ef e validação do modelo físico.
- Scripts SQL: instruções de geração incluídas, mas **não existe script de esquema validado**.
- Outras funcionalidades F004–F009: ficheiros reservados, sem implementação.
- Logout/revogação JWT, rate limiting, email, políticas de autorização por recurso, testes de integração e integrações externas: pendentes.

Este pacote é uma base de desenvolvimento e **não está pronto para produção**.
