var app = angular.module('tablesApp', ['ui.bootstrap']);


//directives
//app.directive('selectOnClick', function () {
//    return {
//        restrict: 'A',
//        link: function (scope, element) {
//            var focusedElement;
//            element.on('click', function () {
//                this.select();
//                focusedElement = this;
//            });
//            element.on('blur', function () {
//                focusedElement = null;
//            });
//        }
//    };
//});

app.directive('stringToNumber', function () {
    return {
        require: 'ngModel',
        link: function (scope, element, attrs, ngModel) {
            ngModel.$parsers.push(function (value) {
                return '' + value;
            });
            ngModel.$formatters.push(function (value) {
                return parseFloat(value);
            });
        }
    };
});
//---


app.controller('tablesCtrl', function ($scope, $http) {
    $scope.init = function () {
        $scope.showLoader = true;
        $http.get('/Home/GetTables').then(function (response) {
            $scope.tables = response.data;
            $scope.showLoader = false;
        });
    }


    $scope.GetTableRecords = function (table, isDependant) {
        if (!isDependant) {
            angular.forEach($scope.tables, function (t) {
                t.selected = false;
            });
            table.selected = true;
        }

        $scope.showLoader = true;
        $http.post('/Home/GetColumns', { table: table }).then(function (response) {
            if (isDependant) {
                table.dependantColumns = response.data;
            } else {
                $scope.columns = response.data;
            }

            if ($scope.newCells != undefined) {
                var idv = $scope.newCells.first(function (nc) { return nc.column.name === $scope.table.identityColumn; });
                $scope.identityValue = idv === null ? null : idv.value;
            }

            $scope.showLoader = false;

            if ((isUndefinedNullOrEmpty($scope.identityValue) && !isDependant) || (!isUndefinedNullOrEmpty($scope.identityValue) && isDependant) || (!isUndefinedNullOrEmpty($scope.identityValue) && !isDependant)) {
                $scope.showLoader = true;
                $http.post('/Home/GetRows', { table: table, columnsCount: isDependant ? table.dependantColumns.length : $scope.columns.length, isDependant: isDependant, identityValue: $scope.identityValue }).then(function (response) {
                    if (isDependant) {
                        table.dependantRows = response.data;
                    } else {
                        $scope.rows = response.data;

                        $scope.showTable = true;

                        $scope.table = table;
                    }

                    $scope.showLoader = false;
                });
            }
        });
    }


    $scope.CheckReferencedRow = function (referencedRows, index, newCell) {
        angular.forEach(referencedRows, function (rr) {
            rr.checked = false;
        });
        referencedRows[index].checked = true;

        newCell.value = referencedRows[index].cells[0]; //TODO get value from only identity column
    }


    $scope.GenerateCells = function (cells) {
        $scope.oldCells = [];
        $scope.newCells = [];

        for (var i = 0; i < $scope.columns.length; i++) {
            $scope.oldCells.push({
                column: $scope.columns[i],
                value: cells === undefined ? '' : cells[i]
            });

            $scope.newCells.push({
                column: $scope.columns[i],
                value: cells === undefined ? '' : cells[i]
            });            
        }
    }


    $scope.GenerateForm = function (cells) {
        $scope.GenerateCells(cells);

        $scope.showLoader = true;
        $http.post('/Home/GetTableReferencedTable', { table: $scope.table }).then(function (response) {
            $scope.referencedTables = response.data;            
            $scope.dateTimeFormat = 'dd.MM.yyyy HH:mm:ss';
            $scope.dateFormat = 'dd.MM.yyyy';
            angular.forEach($scope.newCells, function (nc) {
                if ((nc.column.dataType === 'float' || nc.column.dataType === 'decimal' || nc.column.dataType === 'numeric') && !isUndefinedNullOrEmpty(nc.value)) {
                    nc.value = nc.value.replace(',', '.');
                }
                if (nc.column.dataType === 'datetime' && !isUndefinedNullOrEmpty(nc.value)) {
                    var dd = nc.value.substring(0, 2);
                    var MM = nc.value.substring(3, 5);
                    var yyyy = nc.value.substring(6, 10);
                    var HHmmss = nc.value.substring(11, 19);
                    
                    nc.value = new Date(MM + '.' + dd + '.' + yyyy + ' ' + HHmmss);
                }
                if (nc.column.dataType === 'date' && !isUndefinedNullOrEmpty(nc.value)) {
                    var dd = nc.value.substring(0, 2);
                    var MM = nc.value.substring(3, 5);
                    var yyyy = nc.value.substring(6, 10);

                    nc.value = new Date(MM + '.' + dd + '.' + yyyy);
                }
                angular.forEach($scope.referencedTables, function (rt) {
                    if (nc.column.name === rt.referencedColumn) {
                        nc.hasReference = true;
                        $http.post('/Home/GetColumns', { table: rt }).then(function (response) {
                            nc.referencedColumns = response.data;

                            $http.post('/Home/GetRows', { table: rt, columnsCount: nc.referencedColumns.length, isDependant: true, identityValue: nc.value }).then(function (response) {
                                nc.referencedRows = response.data;
                                var referencedRow = nc.referencedRows[0];
                                if (nc.referencedRows.length === 1) {
                                    $http.post('/Home/GetRows', { table: rt, columnsCount: nc.referencedColumns.length, isDependant: false, identityValue: null }).then(function (response) {
                                        nc.referencedRows = response.data;                                        
                                        angular.forEach(nc.referencedRows, function (rr) {
                                            for (var i = 0; i < referencedRow.cells.length; i++) {
                                                if (referencedRow.cells[i] === rr.cells[i]) {
                                                    rr.checked = true;
                                                    return;
                                                } else {
                                                    rr.checked = false;
                                                    return;
                                                }
                                            }
                                        });
                                        $scope.showLoader = false;
                                    });
                                }
                            });
                        });
                    }
                });
            });
        });

        $scope.showLoader = true;
        $http.post('/Home/GetTableDependantTables', { table: $scope.table }).then(function (response) {
            $scope.dependantTables = response.data;
            $scope.showLoader = false;
        });

        $scope.showForm = true;        

        $scope.action = cells === undefined ? 'INSERT INTO' : 'UPDATE';
    }


    $scope.DeleteRecord = function (cells) {       
        if (confirm('Are you sure you want to delete this record?')) {
            $scope.GenerateCells(cells);

            $scope.action = 'DELETE FROM';

            $scope.SaveRecord();
        }
    }


    $scope.SaveRecord = function () {
        $scope.showLoader = true;
        $http.post('/Home/SaveRecord', { action: $scope.action, table: $scope.table, oldCells: $scope.oldCells, newCells: $scope.newCells }).then(function (response) {
            $scope.showForm = false;

            $scope.GetTableRecords($scope.table, false);
            $scope.showLoader = false;
        });        
    }


    $scope.ChangeCheckboxValue = function (cell) {
        if (cell.value === 'True')
            cell.value = false;
        
        if (cell.value === 'False')
            cell.value = true;
    }
});