package pt.ipca.g15.campuscarpool.ui.perfil

import androidx.compose.foundation.layout.Column
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.res.stringResource
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil

/** Campos comuns aos dois perfis. Não recebe o email: é um dado que o perfil público não tem. */
@Composable fun DadosPerfil(
    nome: String,
    curso: String?,
    instituicao: String?,
    ligacoes: List<LigacaoPerfil>,
    media: Double?
) {
    Text(nome, style = MaterialTheme.typography.titleLarge)
    Text(stringResource(R.string.curso, curso ?: stringResource(R.string.nao_indicado)))
    Text(
        stringResource(
            R.string.instituicao,
            instituicao ?: stringResource(R.string.nao_indicado)
        )
    )
    Text(
        if (media == null) {
            stringResource(R.string.sem_avaliacoes)
        } else {
            stringResource(R.string.avaliacao, media)
        }
    )
    ListaLigacoes(ligacoes)
}

@Composable private fun ListaLigacoes(ligacoes: List<LigacaoPerfil>) {
    val abrir = LocalUriHandler.current
    Text(stringResource(R.string.ligacoes_externas), style = MaterialTheme.typography.titleMedium)
    if (ligacoes.isEmpty()) {
        Text(stringResource(R.string.sem_ligacoes))
        return
    }
    Column {
        ligacoes.forEach { ligacao ->
            val texto = ligacao.descricao ?: ligacao.url
            if (ValidacaoFormularios.eLigacaoWeb(ligacao.url)) {
                // Sem nenhum browser instalado, o sistema recusa; não há nada a fazer aqui.
                TextButton(onClick = { runCatching { abrir.openUri(ligacao.url) } }) {
                    Text(texto)
                }
            } else {
                Text(texto)
            }
        }
    }
}
