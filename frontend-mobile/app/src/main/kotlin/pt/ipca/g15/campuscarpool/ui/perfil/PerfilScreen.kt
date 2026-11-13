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

@Composable fun PerfilScreen(
    estado: EstadoPerfil,
    repetir: () -> Unit,
    editar: () -> Unit,
    verPublico: (String) -> Unit,
    voltar: () -> Unit
) {
    val perfil = estado.perfil
    EcraDeslocavel {
        Titulo(stringResource(R.string.perfil))
        MensagemErro(estado.erro)
        when {
            perfil != null -> {
                DadosPerfil(
                    perfil.nome,
                    perfil.curso,
                    perfil.instituicao,
                    perfil.ligacoesExternas,
                    perfil.classificacaoMedia
                )
                Text(perfil.email)
                Button(onClick = editar) { Text(stringResource(R.string.editar_perfil)) }
                TextButton(onClick = { verPublico(perfil.id) }) {
                    Text(stringResource(R.string.ver_perfil_publico))
                }
            }
            estado.aCarregar -> Text(stringResource(R.string.a_carregar))
            else -> {
                Text(stringResource(R.string.sem_perfil))
                Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
            }
        }
        TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
    }
}
