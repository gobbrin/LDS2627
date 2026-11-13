package pt.ipca.g15.campuscarpool.ui.viagens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.key
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.domain.DataHoraFormatter
import pt.ipca.g15.campuscarpool.domain.model.EstadosViagem
import pt.ipca.g15.campuscarpool.domain.model.ViagemDoCondutor
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable fun MinhasViagensScreen(
    estado: EstadoMinhasViagens,
    repetir: () -> Unit,
    criar: () -> Unit,
    gerir: (String) -> Unit,
    voltar: () -> Unit
) {
    EcraDeslocavel {
        Titulo(stringResource(R.string.minhas_viagens))
        when {
            estado.erro != null -> {
                MensagemErro(estado.erro)
                Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
            }
            !estado.carregada -> Text(stringResource(R.string.a_carregar_viagens))
            else -> {
                Button(onClick = criar, modifier = Modifier.testTag("viagens-criar")) {
                    Text(stringResource(R.string.criar_viagem))
                }
                if (estado.viagens.isEmpty()) Text(stringResource(R.string.sem_viagens))
                estado.viagens.forEach { viagem ->
                    key(viagem.id) { CartaoViagem(viagem, gerir) }
                }
            }
        }
        TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
    }
}

@Composable private fun CartaoViagem(viagem: ViagemDoCondutor, gerir: (String) -> Unit) {
    val descricao = stringResource(R.string.gerir_viagem_de, viagem.origem, viagem.destino)
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Text(
            stringResource(R.string.percurso_valor, viagem.origem, viagem.destino),
            style = MaterialTheme.typography.titleMedium
        )
        Text(rotuloEstado(EstadosViagem.ATIVA))
        Text(DataHoraFormatter.formatar(viagem.partidaEm))
        Text(
            stringResource(
                R.string.lugares_de,
                viagem.lugaresDisponiveis,
                viagem.lugaresOferecidos
            )
        )
        TextButton(
            onClick = { gerir(viagem.id) },
            modifier = Modifier.semantics { contentDescription = descricao }
        ) {
            Text(stringResource(R.string.gerir_viagem))
        }
    }
}

@Composable internal fun rotuloEstado(estado: String): String = when (estado) {
    EstadosViagem.ATIVA -> stringResource(R.string.estado_ativa)
    EstadosViagem.INDISPONIBILIZADA -> stringResource(R.string.estado_indisponibilizada)
    EstadosViagem.CONCLUIDA -> stringResource(R.string.estado_concluida)
    else -> estado
}
