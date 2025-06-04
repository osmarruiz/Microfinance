function getGridOptions(columnDefs, rowData) {
    return {
        columnDefs: columnDefs,
        rowData: rowData,
        pagination: true,
        paginationPageSize: 20,
        paginationPageSizeSelector: false,
        
        defaultColDef: {
            flex: 1,
            minWidth: 100,
            maxWidth: 500,
            wrapText: true,
        },
        localeText: {
            filterOoo: 'Filtrar...',
            textFilter: 'Filtro de Texto',
            numberFilter: 'Filtro Numérico',
            dateFilter: 'Filtro de Fecha',
            setFilter: 'Filtro de Conjunto',
            contains: 'Contiene',
            notContains: 'No contiene',
            equals: 'Igual a',
            notEqual: 'No igual a',
            startsWith: 'Comienza con',
            endsWith: 'Termina con',
            lessThan: 'Menor que',
            lessThanOrEqual: 'Menor o igual que',
            greaterThan: 'Mayor que',
            greaterThanOrEqual: 'Mayor o igual que',
            inRange: 'En rango',
            applyFilter: 'Aplicar',
            resetFilter: 'Restablecer',
            clearFilter: 'Limpiar',
            cancelFilter: 'Cancelar',
            noRowsToShow: 'No hay filas para mostrar',
            page: 'Página',
            to: 'a',
            of: 'de',
            first: 'Primera',
            previous: 'Anterior',
            next: 'Siguiente',
            last: 'Última'
        }
    };
}