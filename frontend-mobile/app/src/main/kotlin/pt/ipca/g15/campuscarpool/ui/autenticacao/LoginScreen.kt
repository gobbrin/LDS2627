package pt.ipca.g15.campuscarpool.ui.autenticacao

import androidx.compose.foundation.layout.Column
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
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

@Composable fun LoginScreen(
    estado: EstadoLogin,
    entrar: (String, String) -> Unit,
    voltar: () -> Unit,
    criarConta: () -> Unit,
    pedirLigacao: (String) -> Unit,
    confirmarConta: () -> Unit
) {
    // O email sobrevive à rotação; a palavra-passe fica de fora do estado restaurável.
    var email by rememberSaveable { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var submeteu by remember { mutableStateOf(false) }
    val ocupado = estado.ocupado
    LaunchedEffect(ocupado) { if (!ocupado && submeteu) password = "" }
    EcraDeslocavel {
        Titulo(stringResource(R.string.entrar))
        estado.emailPorConfirmar?.let { porConfirmar ->
            Column {
                MensagemErro(estado.erro)
                Text(stringResource(R.string.conta_por_confirmar))
                Button(onClick = { pedirLigacao(porConfirmar) }, enabled = !ocupado) {
                    Text(stringResource(R.string.pedir_nova_ligacao))
                }
                TextButton(onClick = confirmarConta, enabled = !ocupado) {
                    Text(stringResource(R.string.ja_tenho_ligacao))
                }
            }
        }
        CampoTexto(
            email,
            { email = it },
            stringResource(R.string.email),
            "login-email",
            !ocupado,
            estado.campoErro("email"),
            KeyboardType.Email
        )
        CampoTexto(
            password,
            { password = it },
            stringResource(R.string.password),
            "login-password",
            !ocupado,
            estado.campoErro("password"),
            KeyboardType.Password,
            oculto = true
        )
        if (estado.emailPorConfirmar == null) MensagemErro(estado.erro)
        Button(
            onClick = {
                submeteu = true
                entrar(email, password)
            },
            enabled = !ocupado && email.isNotBlank() && password.isNotBlank(),
            modifier = Modifier.testTag("login-submit")
        ) {
            Text(stringResource(if (ocupado) R.string.a_entrar else R.string.entrar))
        }
        TextButton(onClick = criarConta, enabled = !ocupado) {
            Text(stringResource(R.string.criar_conta))
        }
        if (estado.emailPorConfirmar == null) {
            TextButton(onClick = { pedirLigacao(email) }, enabled = !ocupado) {
                Text(stringResource(R.string.pedir_nova_ligacao))
            }
        }
        TextButton(onClick = voltar, enabled = !ocupado) { Text(stringResource(R.string.voltar)) }
    }
}

private fun EstadoLogin.campoErro(nome: String): String? {
    val maiuscula = nome.replaceFirstChar(Char::uppercase)
    return (campos[nome] ?: campos[maiuscula])?.joinToString(" ")
}
