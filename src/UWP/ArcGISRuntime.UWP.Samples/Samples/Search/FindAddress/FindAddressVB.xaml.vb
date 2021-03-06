﻿' Copyright 2017 Esri.
'
' Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
' You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
' "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
' language governing permissions and limitations under the License.

Imports System.Reflection
Imports Esri.ArcGISRuntime.Data
Imports Esri.ArcGISRuntime.Geometry
Imports Esri.ArcGISRuntime.Mapping
Imports Esri.ArcGISRuntime.Symbology
Imports Esri.ArcGISRuntime.Tasks.Geocoding
Imports Esri.ArcGISRuntime.UI
Imports Esri.ArcGISRuntime.UI.Controls

Namespace FindAddress

    Partial Public Class FindAddressVB

        ' String array to store the different device location options.
        Private _navigationTypes As String() = New String() {"On", "Re-Center", "Navigation", "Compass"}

        ' The LocatorTask provides geocoding services
        Private _geocoder As LocatorTask

        ' Service Uri to be provided to the LocatorTask (geocoder)
        Private _serviceUri As Uri = New Uri("https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer")

        Public Sub New()
            InitializeComponent()

            ' Setup the control references and execute initialization
            Initialize()
        End Sub

        Private Async Sub Initialize()
            ' Create new Map with basemap
            Dim myMap As New Map(Basemap.CreateImageryWithLabels())

            ' Assign the map to the MapView
            MyMapView.Map = myMap

            ' Initialize the LocatorTask with the provided service Uri
            _geocoder = Await LocatorTask.CreateAsync(_serviceUri)

            ' Enable user controls now that the geocoder is ready
            MyAppBarSearchButton.IsEnabled = True
            MyAppBarSuggestButton.IsEnabled = True

        End Sub

        Private Async Sub updateSearch()
            ' Get the text in the search bar
            Dim enteredText As String = MySearchBox.Text

            ' Clear existing marker
            _MyMapView.GraphicsOverlays.Clear()

            ' Return gracefully if the textbox Is empty Or the geocoder isn't ready
            If String.IsNullOrWhiteSpace(enteredText) Or _geocoder Is Nothing Then Return

            ' Get suggestions based on the input text
            Dim suggestions As IReadOnlyList(Of SuggestResult) = Await _geocoder.SuggestAsync(enteredText)

            ' Stop gracefully if there are no suggestions
            If suggestions.Count < 1 Then Return

            ' Get the full address for the first suggestion
            Dim firstSuggestion As SuggestResult = suggestions.First()
            Dim addresses As IReadOnlyList(Of GeocodeResult) = Await _geocoder.GeocodeAsync(firstSuggestion.Label)

            ' Stop gracefully if the geocoder does not return a result
            If addresses.Count < 1 Then Return

            ' Place a marker on the map - 1. Create the overlay
            Dim resultOverlay As GraphicsOverlay = New GraphicsOverlay()
            ' 2. Get the Graphic to display
            Dim point As Graphic = Await GraphicForPoint(addresses.First().DisplayLocation)
            ' 3. Add the Graphic to the GraphicsOverlay
            resultOverlay.Graphics.Add(point)
            ' 4. Add the GraphicsOverlay to the MapView
            _MyMapView.GraphicsOverlays.Add(resultOverlay)

            ' Update the map extent to show the marker
            Await _MyMapView.SetViewpointGeometryAsync(addresses.First().Extent)
        End Sub

        ''' <summary>
        ''' Creates And returns a Graphic associated with the given MapPoint
        ''' </summary>
        Private Async Function GraphicForPoint(point As MapPoint) As Task(Of Graphic)
            ' Get current assembly that contains the image
            Dim currentAssembly = Me.GetType().GetTypeInfo().Assembly

            ' Get image as a stream from the resources
            ' Picture Is defined as EmbeddedResource And DoNotCopy
            Dim resourceStream = currentAssembly.GetManifestResourceStream(
                "ArcGISRuntime.UWP.Samples.pin_star_blue.png")

            ' Create New symbol using asynchronous factory method from stream
            Dim pinSymbol As PictureMarkerSymbol = Await PictureMarkerSymbol.CreateAsync(resourceStream)
            pinSymbol.Width = 60
            pinSymbol.Height = 60
            ' The map marker Is a pin; offset the pin so that the pinpoint
            '     Is on the point rather than the image's true center
            pinSymbol.OffsetX = pinSymbol.Width / 2
            pinSymbol.OffsetY = pinSymbol.Height / 2
            Return New Graphic(point, pinSymbol)
        End Function

        Private Sub SearchButton_Click(sender As Object, e As RoutedEventArgs)
            updateSearch()
        End Sub

        Private Sub OnSuggestionChosen(sender As Object, e As RoutedEventArgs)
            ' Get the menu item that was clicked
            Dim menuItem As MenuFlyoutItem = sender
            ' Get the text value
            Dim textValue As String = menuItem.Text
            ' Update the search
            MySearchBox.Text = textValue
            updateSearch()
        End Sub

        Private Async Sub MyMapView_GeoViewTapped(sender As Object, e As GeoViewInputEventArgs)
            ' Search for the graphics underneath the user's tap
            Dim results As IReadOnlyList(Of IdentifyGraphicsOverlayResult) = Await _MyMapView.IdentifyGraphicsOverlaysAsync(e.Position, 12, False)

            ' Return gracefully if there was no result
            If results.Count < 1 Then Return
            If results.First().Graphics.Count < 1 Then Return

            ' Reverse geocode to get addresses
            Dim addresses As IReadOnlyList(Of GeocodeResult) = Await _geocoder.ReverseGeocodeAsync(e.Location)

            ' Get the first result
            Dim address As GeocodeResult = addresses.First()
            ' Use the city and region for the Callout Title
            Dim calloutTitle As String = address.Attributes("City").ToString() & ", " & address.Attributes("Region").ToString()
            ' Use the metro area for the Callout Detail
            Dim calloutDetail As String = address.Attributes("MetroArea").ToString()

            ' Use the MapView to convert from the on-screen location to the on-map location
            Dim point As MapPoint = _MyMapView.ScreenToLocation(e.Position)

            ' Define the callout
            Dim calloutBody As CalloutDefinition = New CalloutDefinition(calloutTitle, calloutDetail)

            ' Show the callout on the map at the tapped location
            _MyMapView.ShowCalloutAt(point, calloutBody)
        End Sub

    End Class

End Namespace