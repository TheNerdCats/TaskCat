(function() {
  'use strict';

  angular
    .module('app')
    .controller('orderController', orderController);

  orderController.$inject = ['orderService', '$log'];

  /* @ngInject */
  function orderController(orderService, $log) {
    var vm = this;

    activate();

    function activate() {
      $log.info('Fetching Supported Orders');
      return orderService.getSupportedOrders().then(function(response) {
        $log.info('Fetched ' + response.data.length + ' supported orders');
        vm.orders = response.data;
        return vm.orders;
      }, function(error) {
        $log.error('Unable to load customer data: ' + error.message);
        vm.status = 'Unable to load customer data: ' + error.message;
      });
    }
  }
})();
