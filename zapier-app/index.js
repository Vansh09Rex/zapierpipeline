const orderCreate = require('./creates/order');

module.exports = {
  version: require('./package.json').version,
  platformVersion: require('zapier-platform-core').version,
  creates: {
    [orderCreate.key]: orderCreate,
  },
};
