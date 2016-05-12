var chart = null;
var ctr = 0;
var deviceName = "Laukik";
var myVar = setInterval(updateChart, 1000);
window.onload = function () {

    $("#from").datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 1,
        onClose: function (selectedDate) {
            $("#to").datepicker("option", "minDate", selectedDate);
        },
        onSelect: loadDataFromDateRange
    });
    $("#to").datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 1,
        onClose: function (selectedDate) {
            $("#from").datepicker("option", "maxDate", selectedDate);
        },
        onSelect: loadDataFromDateRange
    });

    chart = new CanvasJS.Chart("chartContainer", {
        animationEnabled: true,
        zoomEnabled: true,
        data: [
                 {
                     type: "spline",
                     dataPoints: [//array
                     ]
                 }
        ]
    });

    chart.render();

    $("#loadSpinner").hide();
}

function updateChart() {
    //$("#loadSpinner").show();
    var jqxhr = $.getJSON("/Timeseries/" + deviceName + "?fromTimeStamp=" + new Date().toISOString() + " &count=10", renderChart);
    jqxhr.always(function () {
        //$("#loadSpinner").hide();
    });

}

function loadDataFromDateRange(selectedDate) {
    if ($("#to").val() != "" & $("#from").val() != "") {
        clearInterval(myVar);
        var toDate = new Date($("#to").val());
        var fromDate = new Date($("#from").val());
        $("#loadSpinner").show();
        var jqxhr = $.getJSON("/Timeseries/" + deviceName + "?fromTimeStamp=" + fromDate.toISOString() + " &toTimeStamp=" + toDate.toISOString(), renderChart);
        jqxhr.always(function () {
            $("#loadSpinner").hide();
        });
    }
}

function renderChart(data) {
    chart.options.data[0].dataPoints = [];
    if (data != null) {
        for (var prop in data) {
            chart.options.data[0].dataPoints.push({ x: new Date(prop), y: parseInt(data[prop]) });
            ctr++;
        };
    }
    chart.render();
}