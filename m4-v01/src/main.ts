import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { appConfig } from './app/app.config';
import { carregarConfiguracao, CONFIGURACAO } from './app/core/configuracao';

carregarConfiguracao()
  .then((config) =>
    bootstrapApplication(App, {
      providers: [...appConfig.providers, { provide: CONFIGURACAO, useValue: config }],
    }),
  )
  .catch(() => {
    const aviso = document.createElement('p');
    aviso.setAttribute('role', 'alert');
    aviso.textContent =
      'Não foi possível iniciar o Campus Carpool. Confirma a configuração ou contacta a equipa de suporte e volta a carregar a página.';
    document.body.replaceChildren(aviso);
  });
