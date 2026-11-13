module.exports = function (config) {
  config.set({
    frameworks: ['jasmine'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-coverage'),
    ],
    browsers: [process.env.KARMA_NO_SANDBOX === '1' ? 'ChromeHeadlessContainer' : 'ChromeHeadless'],
    customLaunchers: {
      ChromeHeadlessContainer: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-dev-shm-usage'],
      },
    },
    reporters: ['progress', 'coverage'],
    coverageReporter: {
      dir: require('node:path').join(__dirname, 'coverage'),
      subdir: '.',
      reporters: [
        { type: 'text-summary' },
        { type: 'html' },
        { type: 'lcovonly' },
        { type: 'json-summary' },
      ],
      check: { global: { lines: 80 } },
    },
    client: { jasmine: { random: true } },
  });
};
