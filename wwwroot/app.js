 (function () {
    'use strict';

    angular
        .module('app', ['ngCookies']);

    angular
        .module('app')
        .controller('mainController', mainController);

    mainController.$inject = ['$log', '$q', '$http', '$cookies'];

    function mainController($log, $q, $http, $cookies) {
        var vm = this;

        vm.isAuthorize = false;
        vm.loading = true;
        vm.login = '';
        vm.password = '';
        vm.authText = '';

        vm.authorize = authorize;

        init();

        return;

        ////////////

        function init() {
            $log.debug('Контроллер загружен');

            checkAuthorize();
        }

        function checkAuthorize() {
            if (!$cookies.getObject('auth')) {
                vm.isAuthorize = false;
                vm.loading = false;
                return;
            }
            
            var get = {
                method: "GET",
                headers: {
                    'Accept': "application/json;",
                    'Authorization': "Bearer " + $cookies.getObject('auth').access_token
                }
            };

            var deferred = $q.defer();

            $http.get('/api/identity/v1.1/secure', get)
				.then(function (data) {
                    $log.log(data.data);

                    vm.authText = data.data;
                    vm.isAuthorize = true;
				})
				.catch(function (error) {
				    deferred.reject(error);
                    //$log.log("Загрузка элемента не удалась", error);
                    
                    refreshToken($cookies.getObject('auth').refresh_token);
                })
                .finally(function () {
                    vm.loading = false;
                });

            return deferred.promise;
        }

        function refreshToken(token) {
            var post = {
                method: "POST",
                headers: {
                    'Accept': "application/json;",
                },
                // params: { 'refreshToken': token }
            };

            var deferred = $q.defer();

            $http.post('/api/identity/v1.1/refresh', { 'refresh_token': token }, post)
				.then(function (data) {
                    $log.log(data.data);

                    $cookies.putObject('auth', { access_token: data.data.access_token, refresh_token: data.data.refresh_token }, { path: '/' });
                    vm.isAuthorize = true;
				})
				.catch(function (error) {
				    deferred.reject(error);
                    $log.log("Загрузка элемента не удалась", error);
                })
                .finally(function () {
                    // vm.loading = false;
                });

            return deferred.promise;
        }

        function authorize() {
            vm.loading = true;
            
            var get = {
                method: "GET",
                headers: {
                    'Accept': "application/json;"
                },
                params: {
                    'login': vm.login,
                    'password': vm.password
                }
            };

            var deferred = $q.defer();

            $http.get('/api/identity/v1.1/token', get)
				.then(function (data) {
                    //deferred.resolve(data.data);
                    $log.log(data);

                    vm.isAuthorize = true;
                    $cookies.putObject('auth', { access_token: data.data.access_token, refresh_token: data.data.refresh_token }, { path: '/' });
				})
				.catch(function (error) {
				    deferred.reject(error);
				    $log.log("Загрузка элемента не удалась", error);
                })
                .finally(function () {
                    vm.loading = false;
                });

            return deferred.promise;
        }
    }
})();