var chart = null;
var ctr = 0;
var deviceName = "Laukik";
window.onload = function() {

    $("#from").datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 1,
        onClose: function(selectedDate) {
            $("#to").datepicker("option", "minDate", selectedDate);
        }
    });
    $("#to").datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 1,
        onClose: function(selectedDate) {
            $("#from").datepicker("option", "maxDate", selectedDate);
        },
        onSelect: function(selectedDate) {
            if ($("#to").datepicker("option", "minDate") != null & $("#from").datepicker("option", "maxDate") != null) {
                var toDate = new Date($("#from").datepicker("option", "maxDate"));
                var fromDate = new Date($("#to").datepicker("option", "minDate"));
                var strfromDate = fromDate.getUTCFullYear()+"-"+ fromDate.getUTCMonth()+"-"+fromDate.getUTCDate()+" "+fromDate.getUTCHours()+":"+fromDate.getUTCMinutes()+":"+fromDate.getUTCSeconds();
                var strtoDate = toDate.getUTCFullYear()+"-"+ toDate.getUTCMonth()+"-"+toDate.getUTCDate()+" "+toDate.getUTCHours()+":"+toDate.getUTCMinutes()+":"+toDate.getUTCSeconds();
                $.getJSON("/Timeseries/" + deviceName + "?fromTimeStamp=" + strfromDate + " &toTimeStamp=" + strtoDate,
                    function(data) {
                        if (data != null) {
                            for (var j = 0; j < data.length; j++) {
                                chart.options.data[0].dataPoints.shift();
                                chart.options.data[0].dataPoints.push({ x: data[j][0], y: data[j][1] });
                                ctr++;
                            };

                            chart.render();
                        }
                    });
            }
        }
    });

    chart = new CanvasJS.Chart("chartContainer",
        {
            animationEnabled: true,
            data: [
                {
                    type: "spline", //change type to bar, line, area, pie, etc
                    showInLegend: true,
                    dataPoints: [{ x: 10, y: 20 }
                    ]
                }
            ],
            legend: {
                cursor: "pointer",
                itemclick: function(e) {
                    if (typeof (e.dataSeries.visible) === "undefined" || e.dataSeries.visible) {
                        e.dataSeries.visible = false;
                    } else {
                        e.dataSeries.visible = true;
                    }
                    chart.render();
                }
            }
        });

    chart.render();
}

function updateChart() {

    //$.getJSON("https://hydra.run.aws-usw02-pr.ice.predix.io/hydrateCurve",
    //  function(data) {

    for (var j = 0; j < 10; j++) {
        chart.options.data[0].dataPoints.shift();
        chart.options.data[0].dataPoints.push({ x: j, y: ctr });
        ctr++;
    };

    chart.render();
    // });
}