package pt.ipca.g15.campuscarpool.ui.autenticacao

import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.stringResource
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.ui.CampoTexto
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable fun ConfirmacaoScreen(
    estado: EstadoConfirmacao,
    autenticado: Boolean,
    confirmar: (String) -> Unit,
    repetir: () -> Unit,
    pedirLigacao: () -> Unit,
    irParaEntrada: () -> Unit,
    voltar: () -> Unit
) {
    var texto by rememberSaveable { mutableStateOf("") }
    EcraDeslocavel {
        Titulo(stringResource(R.string.titulo_confirmacao))
        when (estado.fase) {
            FaseConfirmacao.A_VERIFICAR -> Text(stringResource(R.string.a_confirmar))
            FaseConfirmacao.CONFIRMADA -> {
                Text(stringResource(R.string.conta_confirmada))
                if (!autenticado) {
                    Button(onClick = irParaEntrada) { Text(stringResource(R.string.entrar)) }
                }
            }
            FaseConfirmacao.INICIAL, FaseConfirmacao.FALHOU -> {
                MensagemErro(estado.erro)
                if (estado.repetivel) {
                    Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
                }
                Text(stringResource(R.string.instrucoes_confirmacao))
                CampoTexto(
                    texto,
                    { texto = it },
                    stringResource(R.string.ligacao_ou_codigo),
                    "confirmacao-texto",
                    true,
                    estado.erroEntrada
                )
                Button(
                    onClick = { confirmar(texto) },
                    enabled = texto.isNotBlank(),
                    modifier = Modifier.testTag("confirmacao-submit")
                ) {
                    Text(stringResource(R.string.confirmar_conta))
                }
                // A API só distingue expirada, usada e inválida no texto: oferecem-se os dois caminhos.
                if (!autenticado) {
                    TextButton(onClick = pedirLigacao) {
                        Text(stringResource(R.string.pedir_nova_ligacao))
                    }
                    TextButton(onClick = irParaEntrada) {
                        Text(stringResource(R.string.ir_para_entrada))
                    }
                }
            }
        }
        TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
    }
}
