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
import androidx.compose.ui.text.input.KeyboardType
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.ui.CampoTexto
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable fun ReenvioScreen(
    estado: EstadoReenvio,
    emailInicial: String,
    enviar: (String) -> Unit,
    confirmarConta: () -> Unit,
    irParaEntrada: () -> Unit,
    voltar: () -> Unit
) {
    var email by rememberSaveable { mutableStateOf(emailInicial) }
    EcraDeslocavel {
        Titulo(stringResource(R.string.titulo_reenvio))
        if (estado.concluido != null) {
            Text(estado.concluido)
            Button(onClick = confirmarConta) { Text(stringResource(R.string.ja_tenho_ligacao)) }
            TextButton(onClick = irParaEntrada) { Text(stringResource(R.string.ir_para_entrada)) }
        } else {
            CampoTexto(
                email,
                { email = it },
                stringResource(R.string.email),
                "reenvio-email",
                !estado.ocupado,
                estado.erroEmail,
                KeyboardType.Email
            )
            MensagemErro(estado.erro)
            Button(
                onClick = { enviar(email) },
                enabled = !estado.ocupado,
                modifier = Modifier.testTag("reenvio-submit")
            ) {
                Text(stringResource(if (estado.ocupado) R.string.a_enviar else R.string.enviar))
            }
        }
        TextButton(onClick = voltar, enabled = !estado.ocupado) {
            Text(stringResource(R.string.voltar))
        }
    }
}
