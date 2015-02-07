
$(document).ready(function () {

    var urlString = GetUrlForService();
    mainRow.style.display = "none";
    errNoEvents.style.display = "none";

    $('#divSingleEvent').hide();
    $('#mainRow').hide();
    $.ajax({
        url: urlString,
    }).done(function (res) {
        masterEvents = res;

        if (masterEvents.length == 0) {
            $('#progressBar').hide();
            $('#errNoEvents').show();
        }

        else {

            for (var i = 0; i < masterEvents.length; i++) {

                if (jQuery.inArray(masterEvents[i].Source, listOfSources) == -1) {
                    listOfSources.push(masterEvents[i].Source);
                }

                if (jQuery.inArray(masterEvents[i].Level, listOfLevels) == -1) {
                    listOfLevels.push(masterEvents[i].Level);
                }

                viewModel.serverSideEvents.push(masterEvents[i]);
                $('#mainRow').show();
                $('#progressBar').hide();
            }


            listOfSources.forEach(addCheckboxSources);
            listOfLevels.forEach(addCheckboxLevels);

            ko.applyBindings(viewModel);

            $('#lblEventCount').text(masterEvents.length);

            $('#btnSearch').click(function () {

                $('#divSingleEvent').hide();
                $('#progressBar').show();

                setTimeout(function () {
                    searchTable($('#searchText').val());
                }, 1000);


                $('#divSingleEvent').text('');


            });

            $('#btnClear').click(function () {

                $('#divSingleEvent').hide();
                $('#progressBar').show();
                setTimeout(function () {
                    //Call this portion of code asynchronously to update the UI
                    viewModel.serverSideEvents.removeAll();
                    for (var i = 0; i < masterEvents.length; i++) {
                        viewModel.serverSideEvents.push(masterEvents[i]);
                    }

                    $('#lblCurrentEventCount').text('');
                    $('#txtEventId').val('');
                    $('#searchText').val('');
                    $('#divSingleEvent').text('');

                    //Select the checkbox controls
                    $('#checkBoxlistOfSources').each(function (index, row) {
                        var inputCheckBoxElement = $(this).find(':input').each(function (index) {
                            this.checked = true;
                        });
                    });


                    $('#checkBoxlistOfLevels').each(function (index, row) {
                        var inputCheckBoxElement = $(this).find(':input').each(function (index) {
                            this.checked = true;
                        });
                    });

                    $('#progressBar').hide();
                }, 1000);


            });
        }

    });

    // Attach the click event to all the rows
    $('#grdEvents').on('click', 'tr', function (event) {
        var tr = $(this);
        var hiddenText = tr.find('.lblDesc').text();
        $('#grdEvents tr').removeClass('selectedEvent');
        tr.addClass('selectedEvent');
        $('#divSingleEvent')
            .text(hiddenText)
            .show(500);
    });

});

function RowClick(event) {

}

function GetUrlForService()
{  
    urltoService = 'api/events';  
    return urltoService;
}

function addCheckboxSources(name) {
    var container = $('#checkBoxlistOfSources');
    var inputs = container.find('input');
    var id = inputs.length + 1;

    var prefix = 'cbsrc';
    $('<input />', { type: 'checkbox', id: prefix + id, value: name, checked: true }).appendTo(container);
    $('<label />', { 'for': prefix + id, text: name }).appendTo(container);
    $('<br />').appendTo(container);
}

function addCheckboxLevels(name) {
    var container = $('#checkBoxlistOfLevels');
    var inputs = container.find('input');
    var id = inputs.length + 1;
    var levelText = "";

    if (String(name) == "0") {
        levelText = "Information";
    }

    else if (String(name) == "1") {
        levelText = "Verbose";
    }

    else if (String(name) == "2") {
        levelText = "Warning";
    }
    else if (String(name) == "3") {
        levelText = "Error";
    }

    var prefix = 'cblvl';
    $('<input />', { type: 'checkbox', id: prefix + id, value: name, checked: true }).appendTo(container);
    $('<label />', { 'for': prefix + id, text: levelText }).appendTo(container);
    $('<br />').appendTo(container);
}

function searchTable(inputVal) {

    $('#progressBar').show()

    var eventCount = 0;

    var listofSourcesSelected = [];
    var listofLevelsSelected = [];

    $('#checkBoxlistOfSources').each(function (index, row) {
        var inputCheckBoxElement = $(this).find(':input').each(function (index) {

            if (this.checked) {
                listofSourcesSelected.push($(this).val());
            }
        });


    });


    $('#checkBoxlistOfLevels').each(function (index, row) {
        var inputCheckBoxElement = $(this).find(':input').each(function (index) {

            if (this.checked) {
                listofLevelsSelected.push($(this).val());
            }
        });
    });

    //alert(listofSourcesSelected.join(','));
    //alert(listofLevelsSelected.join(','));


    viewModel.serverSideEvents.removeAll();

    for (var i = 0; i < masterEvents.length; i++) {

        if (jQuery.inArray(masterEvents[i].Source, listofSourcesSelected) > -1) {
            if (jQuery.inArray(masterEvents[i].Level, listofLevelsSelected) > -1) {

                var searchText = $('#searchText').val();

                var regExp = new RegExp(searchText, 'i');
                if (regExp.test(masterEvents[i].Description)) {

                    var txtEventId = $('#txtEventId').val();
                    if (txtEventId == '') {

                        viewModel.serverSideEvents.push(masterEvents[i]);
                        eventCount++;
                    }
                    else {
                        var eventIDsArray = [];
                        eventIDsArray = txtEventId.split(',')

                        if (jQuery.inArray(masterEvents[i].EventID, eventIDsArray) > -1) {

                            viewModel.serverSideEvents.push(masterEvents[i]);
                            eventCount++;
                        }
                    }
                }

            }

        }

    }

    $('#lblCurrentEventCount').text(eventCount + " of ");
    $('#progressBar').hide();
    
}

var masterEvents = [];

var listOfSources = [];

var listOfLevels = [];



var viewModel = {
    serverSideEvents: ko.observableArray([]),
};

