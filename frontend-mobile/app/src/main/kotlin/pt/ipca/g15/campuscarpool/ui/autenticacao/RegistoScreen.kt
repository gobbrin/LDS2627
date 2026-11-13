package pt.ipca.g15.campuscarpool.ui.autenticacao

import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
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

@Composable fun RegistoScreen(
    estado: EstadoRegisto,
    registar: (String, String, String, String) -> Unit,
    irParaEntrada: () -> Unit,
    pedirLigacao: (String) -> Unit,
    confirmarConta: () -> Unit,
    voltar: () -> Unit
) {
    if (estado.concluido != null || estado.emailNaoEnviado != null) {
        ContaCriada(estado, irParaEntrada, pedirLigacao, confirmarConta)
        return
    }
    var nome by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var confirmacao by remember { mutableStateOf("") }
    val ativo = !estado.ocupado
    EcraDeslocavel {
        Titulo(stringResource(R.string.titulo_registo))
        CampoTexto(
            nome,
            { nome = it },
            stringResource(R.string.nome),
            "registo-nome",
            ativo,
            estado.erros["nome"]
        )
        CampoTexto(
            email,
            { email = it },
            stringResource(R.string.email),
            "registo-email",
            ativo,
            estado.erros["email"],
            KeyboardType.Email
        )
        CampoTexto(
            password,
            { password = it },
            stringResource(R.string.password),
            "registo-password",
            ativo,
            estado.erros["password"],
            KeyboardType.Password,
            oculto = true,
            ajuda = stringResource(R.string.regra_password)
        )
        CampoTexto(
            confirmacao,
            { confirmacao = it },
            stringResource(R.string.confirmar_password),
            "registo-confirmacao",
            ativo,
            estado.erros["confirmacao"],
            KeyboardType.Password,
            oculto = true
        )
        MensagemErro(estado.erro)
        if (estado.emailJaRegistado) {
            Button(onClick = irParaEntrada) { Text(stringResource(R.string.entrar)) }
            TextButton(onClick = { pedirLigacao(estado.email) }) {
                Text(stringResource(R.string.pedir_nova_ligacao))
            }
        }
        Button(
            onClick = { registar(nome, email, password, confirmacao) },
            enabled = ativo,
            modifier = Modifier.testTag("registo-submit")
        ) {
            Text(stringResource(if (ativo) R.string.criar_conta else R.string.a_criar_conta))
        }
        TextButton(onClick = irParaEntrada, enabled = ativo) {
            Text(stringResource(R.string.ja_tenho_conta))
        }
        TextButton(onClick = voltar, enabled = ativo) { Text(stringResource(R.string.voltar)) }
    }
}

@Composable private fun ContaCriada(
    estado: EstadoRegisto,
    irParaEntrada: () -> Unit,
    pedirLigacao: (String) -> Unit,
    confirmarConta: () -> Unit
) {
    EcraDeslocavel {
        if (estado.emailNaoEnviado != null) {
            Titulo(stringResource(R.string.falta_confirmacao))
            MensagemErro(estado.emailNaoEnviado)
        } else {
            Titulo(stringResource(R.string.verifica_email))
            Text(estado.concluido.orEmpty())
        }
        Button(onClick = confirmarConta) { Text(stringResource(R.string.ja_tenho_ligacao)) }
        TextButton(onClick = { pedirLigacao(estado.email) }) {
            Text(stringResource(R.string.pedir_nova_ligacao))
        }
        TextButton(onClick = irParaEntrada) { Text(stringResource(R.string.ir_para_entrada)) }
    }
}
