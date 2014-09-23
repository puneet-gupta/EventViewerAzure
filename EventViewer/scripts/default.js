
$(document).ready(function () {

    var urlString = GetUrlForService();
    mainRow.style.display = "none";
    $('#divSingleEvent').hide();
    $('#mainRow').hide();
    $.ajax({
        url: urlString,
    }).done(function (res) {
        masterEvents = res;
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
            setTimeout(function ()
            {
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

                // Attach the click event to all the rows
                $('#grdEvents tr').click(function (event) {
                    var hiddenText = $('#lblDesc', this).text();
                    $('#grdEvents tr').each(function (index, element) {
                        $(this).removeClass('selectedEvent');

                    });
                    $(this).addClass('selectedEvent');
                    $('#divSingleEvent').html(nl2br(hiddenText));
                    $('#divSingleEvent').show(500);
                });
                // Attach the click event to all the rows

                $('#progressBar').hide();               
            }, 1000);

            
        });


        // Attach the click event to all the rows
        $('#grdEvents tr').click(function (event) {
            var hiddenText = $('#lblDesc', this).text();
            $('#grdEvents tr').each(function (index, element) {
                $(this).removeClass('selectedEvent');

            });
            $(this).addClass('selectedEvent');
            $('#divSingleEvent').html(nl2br(hiddenText));
            $('#divSingleEvent').show(500);
        });
        // Attach the click event to all the rows

    });

});

function RowClick(event) {

}

function GetUrlForService()
{  
    urltoService = 'api/events';  
    return urltoService;
}

function nl2br(text) {
    var nl = '\n';
    text = unescape(escape(text).replace(/%0A%0A/g, '<br/>'));
    return text;
}

function nl2br2(text) {
    text = escape(text);
    if (text.indexOf('%0D%0A') > -1) {
        re_nlchar = /%0D%0A/g;
    } else if (text.indexOf('%0A') > -1) {
        re_nlchar = /%0A/g;
    } else if (text.indexOf('%0D') > -1) {
        re_nlchar = /%0D/g;
    }
    return unescape(text.replace(re_nlchar, '<br />'));
}

function addCheckboxSources(name) {
    var container = $('#checkBoxlistOfSources');
    var inputs = container.find('input');
    var id = inputs.length + 1;

    $('<input />', { type: 'checkbox', id: 'cb' + id, value: name, checked: true }).appendTo(container);
    $('<label />', { 'for': 'cb' + id, text: name }).appendTo(container);
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

    $('<input />', { type: 'checkbox', id: 'cb' + id, value: name, checked: true }).appendTo(container);
    $('<label />', { 'for': 'cb' + id, text: levelText }).appendTo(container);
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

    // Attach the click event to all the rows
    $('#grdEvents tr').click(function (event) {
        var hiddenText = $('#lblDesc', this).text();
        $('#grdEvents tr').each(function (index, element) {
            $(this).removeClass('selectedEvent');

        });
        $(this).addClass('selectedEvent');
        $('#divSingleEvent').html(nl2br(hiddenText));
        $('#divSingleEvent').show(500);
    });
    // Attach the click event to all the rows

    $('#lblCurrentEventCount').text(eventCount + " of ");
    $('#progressBar').hide();

    
}

var masterEvents = [];

var listOfSources = [];

var listOfLevels = [];



var viewModel = {
    serverSideEvents: ko.observableArray([]),
};

