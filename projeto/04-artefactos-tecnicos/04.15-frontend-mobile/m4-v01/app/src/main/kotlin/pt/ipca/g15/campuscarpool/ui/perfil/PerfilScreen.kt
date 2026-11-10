package pt.ipca.g15.campuscarpool.ui.perfil
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
@Composable fun PerfilScreen(
    perfil: PerfilProprio?,
    carregando: Boolean,
    repetir: () -> Unit,
    voltar: () -> Unit
) {
    Column(
        Modifier.verticalScroll(rememberScrollState()).padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(stringResource(R.string.perfil))
        when {
            carregando -> Text(stringResource(R.string.a_carregar))
            perfil != null -> {
                val media = perfil.classificacaoMedia
                Text(perfil.nome)
                Text(perfil.email)
                Text(
                    stringResource(
                        R.string.curso,
                        perfil.curso ?: stringResource(R.string.nao_indicado)
                    )
                )
                Text(
                    stringResource(
                        R.string.instituicao,
                        perfil.instituicao ?: stringResource(R.string.nao_indicado)
                    )
                )
                Text(
                    if (media == null) {
                        stringResource(R.string.sem_avaliacoes)
                    } else {
                        stringResource(R.string.avaliacao, media)
                    }
                )
            }
            else -> {
                Text(stringResource(R.string.sem_perfil))
                Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
            }
        }
        TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
    }
}
