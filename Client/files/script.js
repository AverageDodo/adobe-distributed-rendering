//#region Utility

function logToFile(msg) {
    var logFile = new File("C:\\ProgramData\\Adobe\\ame-script-log.txt");
    logFile.open("a");
    logFile.writeln(msg);
    logFile.close();
}

function notifyRenderComplete() {
    try {
        var notifyScript = new File("/*{PATH}*/");
        var wasExecuted = notifyScript.execute();
        logToFile("Was notify script launched: " + wasExecuted);
    } catch (error) {
        logToFile(error);
    }
}

// From https://github.com/douglascrockford/JSON-js/blob/master/json2.js
// The normal JSON object is not supported in AME so we have to use this.
function parseJSON(jsonString) {
    return eval('(' + jsonString + ')');
}

// basic JSON converter function. Should be enough for what we are doing here
function stringify(obj) {
    if (typeof obj === "string") {
        return '"' + obj.replace(/"/g, '\\"') + '"';
    }
    if (typeof obj === "number" || typeof obj === "boolean" || obj === null) {
        return String(obj);
    }
    if (obj instanceof Array) {
        var arr = [];
        for (var i = 0; i < obj.length; i++) {
            arr.push(stringify(obj[i]));
        }
        return "[" + arr.join(",") + "]";
    }
    if (typeof obj === "object") {
        var props = [];
        for (var key in obj) {
            if (obj.hasOwnProperty(key)) {
                props.push('"' + key + '":' + stringify(obj[key]));
            }
        }
        return "{" + props.join(",") + "}";
    }
    return "null";
}

//#endregion

var jobData = /*{DATA}*/;

logToFile("-----------------------------------------------------------");
logToFile("Log for job with GUID '" + jobData.Guid + "'\n");

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
        logToFile("No DL items found at root of project: " + jobData.ProjectPath);
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
        logToFile("encoderWrapper is not valid");
    }

    var encoderHost = app.getEncoderHost();
    if (encoderHost) {
        encoderHost.runBatch();

        var encodeCompleteEvent = AMEExportEvent.onEncodeComplete;
        app.getExporter().addEventListener(
            encodeCompleteEvent,
            function (eventObj) {
                logToFile(
                    "Exit status: " + eventObj.encodeCompleteStatus
                );

                notifyRenderComplete();
                $.exit();
            },
            {once: true}
        );
    } else {
        logToFile("encoderHost is not valid");
    }
} else {
    logToFile("frontend is not valid");
}
