# Data Sources

Data Sources allow the program to read data frames from various different source. This document will discuss how to add a new data source to OptiSpeech 2. Typically this'll need to be done to support a new EMA system. If you just want to support loading in a new type of file, you should instead create a new [File Reader](./filereaders.md). 

## Implement [DataSourceReader](../api/Optispeech.Data.DataSourceReader.yml)

The core of the new data source is the actual data reader itself, usually saved in `Assets/Scripts/Data/Sources`. It has the following abstract methods that'll need to be implemented:

- [`DataSourceReaderStatus GetCurrentStatus()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_GetCurrentStatus) - This function is needed to determine when the data source is available. This should be where you determine if any hardware or software requirements of this data source are met. You can pass [DataSourceReaderStatus](../api/Optispeech.Data.DataSourceReader.DataSourceReaderStatus.yml).UNKNOWN and later invoke [statusChangeEvent](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_statusChangeEvent) if you need to asynchronously check for requirements.
- [`DataFrame ReadFrame()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_ReadFrame) - This is the function that should return the next frame from the data source. This runs in a separate thread so waiting until the next frame is available is allowed. Several fields in the DataFrame are filled in while processing each frame. The only fields required to be set are [sensorData](../api/Optispeech.Data.DataFrame.yml#Optispeech_Data_DataFrame_sensorData), and [timestamp](../api/Optispeech.Data.DataFrame.yml#Optispeech_Data_DataFrame_timestamp) if the data source reader returns `true` from [IsTimestampProvided()](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_IsTimestampProvided).

Click each link to see more specific information on implementing each function.

Additionally, there are several virtual functions that can be overriden for more advanced control of the data source:

- [`bool IsTimestampProvided()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_IsTimestampProvided) - By default the data source expects [DataFrame](../api/Optispeech.Data.DataFrame.yml)'s coming from [ReadFrame](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_ReadFrame) to contain timestamps, but returning `false` in this function will make the [DataSourceReader](../api/Optispeech.Data.DataSourceReader.yml) set the timestamp itself
- [`bool AreTargetsConfigurable()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_AreTargetsConfigurable) - By default targets can be added, modified, and removed by the researcher. Returning `false` in this function will make all the target configs read-only
- [`bool AreSensorsConfigurable()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_AreSensorsConfigurable) - By default sensors can have their roles and post-offsets changed by the researcher. Returning `false` in this function will make all the sensors read-only
- [`SensorConfiguration[] GetDefaultSensorConfigurations()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_GetDefaultSensorConfigurations) - Returns the default sensor configurations to have initially
- [`void StartThread()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_StartThread) - Function that's called when the data source is selected. Make sure to still call `base.StartThread()`. This can be used to, e.g., tell the data source to start sending data frames
- [`void Cleanup()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_Cleanup) - Function that's called when the data source is de-selected.
- [`void StartSweep(string folderPath, string sweepName)`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_StartSweep_System_String_System_String_) - Function that's called whenever a sweep starts, which can be used to, e.g., start a sweep on the data source so they're synced
- [`void StopSweep()`](../api/Optispeech.Data.DataSourceReader.yml#Optispeech_Data_DataSourceReader_StopSweep) - Function that's called whenever a sweep ends

If any part is confusing, it's recommended to read through any of the existing data source readers to use as a reference. Notably, there's a utility class [TcpClientController](../api/Optispeech.Data.TcpClientController.yml) that is recommended for any data source reader that uses a TCP connection.

## Create a [DataSourceDescription](../api/Optispeech.Data.DataSourceDescription.yml)

This is a scriptable object that will tell the program about this new data source. It must be inside a folder called `Data Source Descriptions` inside of any `Resources` folder in `Assets`. To create the [DataSourceDescription](../api/Optispeech.Data.DataSourceDescription.yml), go to `Optispeech > Data Source` in the create menu. The create menu is accessed through the plus sign in the project panel or by right clicking in a folder and hovering over `Create`. You must then specify the name of the data source and give it a prefab. The only requirement for this prefab is that it include the implementation of [DataSourceReader](../api/Optispeech.Data.DataSourceReader.yml) made in the previous step. It can contain anything else you need for this data source. This prefab will be instantiated in the Source Settings Accordion, so any [TogglePanel](../api/Optispeech.UI.TogglePanel.yml)s in the prefab will automatically be in that accordion.

Once this is setup, the data source should be detected and appear in the Data Sources panel.

## Add documentation for the new data source

Make sure to add a page in the Researcher Manual describing when and how to use the new data source!

## Add a File Reader

If this data source has its own concept of "sweeps", consider adding a [FileReader](../api/Optispeech.Data.FileReaders.FrameReader.FileReader.yml) to read that sweep data in the [FileDataSource](../api/Optispeech.Data.Sources.FileDataSource.yml) as well. See [File Readers](./filereaders.md) for details on adding a new one.
