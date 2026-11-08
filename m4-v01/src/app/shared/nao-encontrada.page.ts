import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  selector: 'app-nao-encontrada',
  imports: [RouterLink],
  template: `<section class="panel">
    <p class="eyebrow">404</p>
    <h1>Este caminho não existe.</h1>
    <p>Verifica o endereço ou regressa ao início.</p>
    <a routerLink="/">Voltar ao início</a>
  </section>`,
})
export class NaoEncontradaPage {}
