import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SessaoService } from '../../core/sessao.service';
@Component({
  selector: 'app-inicio-page',
  imports: [RouterLink],
  template: `<section class="hero">
      <div>
        <p class="eyebrow">CAMPUS CARPOOL</p>
        <h1>O caminho para o campus<br /><em>faz-se em companhia.</em></h1>
        <p class="lead">Uma comunidade académica mais próxima, uma viagem de cada vez.</p>
        @if (sessao.autenticado()) {
          <a class="button primary" routerLink="/perfil"
            >Consultar o meu perfil <span aria-hidden="true">↗</span></a
          >
        } @else {
          <a class="button primary" routerLink="/entrar"
            >Entrar na comunidade <span aria-hidden="true">↗</span></a
          >
        }
      </div>
      <div class="journey" aria-hidden="true">
        <div class="journey-label">UM PERCURSO PARTILHADO</div>
        <div class="route-point">
          <span>○</span>
          <div>O teu ponto de partida<small>Uma nova companhia</small></div>
        </div>
        <div class="route-line"></div>
        <div class="route-point">
          <span>●</span>
          <div>Campus<small>O mesmo destino</small></div>
        </div>
        <div class="journey-footer">Menos distância entre nós.</div>
      </div>
    </section>
    <section class="principles" aria-label="A comunidade">
      <div>
        <span>01</span>
        <h2>Comunidade académica</h2>
        <p>Um ponto de encontro entre estudantes.</p>
      </div>
      <div>
        <span>02</span>
        <h2>Percursos em comum</h2>
        <p>Partilhar o caminho aproxima-nos.</p>
      </div>
      <div>
        <span>03</span>
        <h2>Mais companhia</h2>
        <p>A viagem também faz parte do dia.</p>
      </div>
    </section>`,
})
export class InicioPage {
  readonly sessao = inject(SessaoService);
}
