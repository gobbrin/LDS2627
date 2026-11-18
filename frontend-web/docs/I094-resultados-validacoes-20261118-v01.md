# I094 — Registo suplementar de validação local — v01

**Data dos resultados comunicados:** 18/11/2026  
**Origem:** saídas PowerShell enviadas pelo grupo nesta conversa.  
**Natureza:** atualização documental; não substitui logs/relatórios anexados nem revisão ou auditoria formal.

## Análise estática / build Android observados
- `:app:ktlintFormat`: `BUILD SUCCESSFUL`.
- `:app:ktlintCheck`: `BUILD SUCCESSFUL`.
- `:app:testDebugUnitTest`: `BUILD SUCCESSFUL`.
- `:app:verificarCoberturaApp`: `BUILD SUCCESSFUL` (a regra configurada de 80% passou).
- `:app:lintDebug`: `BUILD SUCCESSFUL`.
- `:app:assembleDebug`: `BUILD SUCCESSFUL`.
- `:app:connectedDebugAndroidTest`: 6 testes instrumentados aprovados no emulador.

**Conclusão local:** os gates estáticos e de cobertura Android passaram na cópia de trabalho. O ZIP não contém as alterações de formatação que `ktlintFormat` possa ter escrito localmente.

## Rastreabilidade e limitações
- Estes resultados referem-se à cópia de trabalho local do utilizador, não necessariamente ao commit ou patch original incluído neste ZIP.
- Esta atualização não modifica `alteracoes.patch` nem outros ficheiros técnicos. As modificações feitas pelo `ktlintFormat` na máquina do utilizador não estavam disponíveis para exportação nesta conversa.
- Para formalização, associar os logs, o commit SHA, a branch, a referência da PR e a validação dos responsáveis.
- **Versão documental:** `v01`. **Merge Request:** [referência do MR a inserir].
