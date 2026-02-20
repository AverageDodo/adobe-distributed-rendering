// Render fragment data is inserted here
var jobData = /*{DATA}*/;

// The value of ticksPerSecond is predefined in premiere pro and ame.
// For more information please have a look into https://ppro-scripting.docsforadobe.dev/other/time.html
var ticksPerSecond = 254016000000;
var startTimeInTicks = (jobData.StartTimeInMilliseconds / 1000) * ticksPerSecond;
var endTimeInTicks = startTimeInTicks + (jobData.DurationInMilliseconds / 1000) * ticksPerSecond;

var startTimeinTicksStr = String(startTimeInTicks);
var endTimeInTicksStr = String(endTimeInTicks);

app.assertToConsole();
var frontend = app.getFrontend();
if (frontend) {
    var dlItems = frontend.getDLItemsAtRoot(jobData.ProjectPath);
    if (!dlItems || dlItems.length === 0) {
        $.writeln("No DL items found at root of project: " + jobData.ProjectPath);
    }

    var encoderWrapper = frontend.addDLToBatch(
        jobData.ProjectPath,
        "H.264",
        jobData.PresetPath,
        dlItems[0],
        jobData.DestinationPath
    );

    if (encoderWrapper) {
        encoderWrapper.setWorkAreaInTicks(
            2,
            startTimeinTicksStr,
            endTimeInTicksStr
        );
    } else {
        $.writeln("encoderWrapper is not valid");
    }

    var encoderHost = app.getEncoderHost();
    if (encoderHost) {
        encoderHost.runBatch();

        var encodeCompleteEvent = AMEExportEvent.onEncodeComplete;
        app.getExporter().addEventListener(
            encodeCompleteEvent,
            function (eventObj) {
                $.writeln(
                    "Exit status: " + eventObj.encodeCompleteStatus
                );

                app.quit();
            }
        );
    } else {
        $.writeln("encoderHost is not valid");
    }
} else {
    $.writeln("frontend is not valid");
}
