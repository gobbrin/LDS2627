import { Component, input } from '@angular/core';
@Component({
  selector: 'app-erro-formulario',
  template: `@if (erros().length) {
    <ul class="field-error" role="alert">
      @for (erro of erros(); track $index) {
        <li>{{ erro }}</li>
      }
    </ul>
  }`,
})
export class ErroFormularioComponent {
  readonly erros = input<string[]>([]);
}
