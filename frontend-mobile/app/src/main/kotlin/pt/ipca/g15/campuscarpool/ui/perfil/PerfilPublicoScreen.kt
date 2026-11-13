package pt.ipca.g15.campuscarpool.ui.perfil

import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.res.stringResource
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable
fun PerfilPublicoScreen(estado: EstadoPerfilPublico, repetir: () -> Unit, voltar: () -> Unit) {
    val perfil = estado.perfil
    EcraDeslocavel {
        Titulo(stringResource(R.string.perfil_publico))
        when {
            perfil != null -> DadosPerfil(
                perfil.nome,
                perfil.curso,
                perfil.instituicao,
                perfil.ligacoesExternas,
                perfil.classificacaoMedia
            )
            estado.aCarregar -> Text(stringResource(R.string.a_carregar))
            estado.naoEncontrado -> Text(stringResource(R.string.perfil_nao_encontrado))
            else -> {
                MensagemErro(estado.erro)
                Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
            }
        }
        TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
    }
}
