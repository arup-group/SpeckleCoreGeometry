﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using Newtonsoft.Json;

namespace SpeckleCoreGeometryRhino
{
  public static partial class Conversions
  {
    // Convenience methods point:
    public static double[ ] ToArray( this Point3d pt )
    {
      return new double[ ] { pt.X, pt.Y, pt.Z };
    }

    public static double[ ] ToArray( this Point2d pt )
    {
      return new double[ ] { pt.X, pt.Y };
    }

    public static double[ ] ToArray( this Point2f pt )
    {
      return new double[ ] { pt.X, pt.Y };
    }

    public static Point3d ToPoint( this double[ ] arr )
    {
      return new Point3d( arr[ 0 ], arr[ 1 ], arr[ 2 ] );
    }


    // Mass point converter
    public static Point3d[ ] ToPoints( this IEnumerable<double> arr )
    {
      if ( arr.Count() % 3 != 0 ) throw new Exception( "Array malformed: length%3 != 0." );

      Point3d[ ] points = new Point3d[ arr.Count() / 3 ];
      var asArray = arr.ToArray();
      for ( int i = 2, k = 0; i < arr.Count(); i += 3 )
        points[ k++ ] = new Point3d( asArray[ i - 2 ], asArray[ i - 1 ], asArray[ i ] );

      return points;
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point3d> points )
    {
      return points.SelectMany( pt => pt.ToArray() ).ToArray();
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point2f> points )
    {
      return points.SelectMany( pt => pt.ToArray() ).ToArray();
    }

    // Convenience methods vector:
    public static double[ ] ToArray( this Vector3d vc )
    {
      return new double[ ] { vc.X, vc.Y, vc.Z };
    }

    public static Vector3d ToVector( this double[ ] arr )
    {
      return new Vector3d( arr[ 0 ], arr[ 1 ], arr[ 2 ] );
    }

    // Points
    // GhCapture?
    public static SpecklePoint ToSpeckle( this Point3d pt )
    {
      return new SpecklePoint( pt.X, pt.Y, pt.Z );
    }
    // Rh Capture?
    public static Rhino.Geometry.Point ToNative( this SpecklePoint pt )
    {
      var myPoint = new Rhino.Geometry.Point( new Point3d( pt.Value[ 0 ], pt.Value[ 1 ], pt.Value[ 2 ] ) );
      myPoint.UserDictionary.ReplaceContentsWith( pt.Properties.ToNative() );
      return myPoint;
    }

    public static SpecklePoint ToSpeckle( this Rhino.Geometry.Point pt )
    {
      return new SpecklePoint( pt.Location.X, pt.Location.Y, pt.Location.Z, properties: pt.UserDictionary.Count != 0 ? pt.UserDictionary.ToSpeckle( root: pt ) : null );
    }

    // Vectors
    public static SpeckleVector ToSpeckle( this Vector3d pt )
    {
      return new SpeckleVector( pt.X, pt.Y, pt.Z );
    }

    public static Vector3d ToNative( this SpeckleVector pt )
    {
      return new Vector3d( pt.Value[ 0 ], pt.Value[ 1 ], pt.Value[ 2 ] );
    }

    // Interval
    public static SpeckleInterval ToSpeckle( this Interval interval )
    {
      var speckleInterval = new SpeckleInterval( interval.T0, interval.T1 );
      speckleInterval.GenerateHash();
      return speckleInterval;
    }

    public static Interval ToNative( this SpeckleInterval interval )
    {
      return new Interval( ( double ) interval.Start, ( double ) interval.End );
    }

    // Interval2d
    public static SpeckleInterval2d ToSpeckle( this UVInterval interval )
    {
      return new SpeckleInterval2d( interval.U.ToSpeckle(), interval.V.ToSpeckle() );
    }

    public static UVInterval ToNative( this SpeckleInterval2d interval )
    {
      return new UVInterval( interval.U.ToNative(), interval.V.ToNative() );
    }

    // Plane
    public static SpecklePlane ToSpeckle( this Plane plane )
    {
      return new SpecklePlane( plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle() );
    }

    public static Plane ToNative( this SpecklePlane plane )
    {
      var returnPlane = new Plane( plane.Origin.ToNative().Location, plane.Normal.ToNative() );
      returnPlane.XAxis = plane.Xdir.ToNative();
      returnPlane.YAxis = plane.Ydir.ToNative();
      return returnPlane;
    }

    // Line
    // Gh Line capture
    public static SpeckleLine ToSpeckle( this Line line )
    {
      return new SpeckleLine( ( new Point3d[ ] { line.From, line.To } ).ToFlatArray() );
    }

    // Rh Line capture
    public static SpeckleLine ToSpeckle( this LineCurve line )
    {
      return new SpeckleLine( ( new Point3d[ ] { line.PointAtStart, line.PointAtEnd } ).ToFlatArray(), properties: line.UserDictionary.ToSpeckle( root: line ) ) { Domain = line.Domain.ToSpeckle() };
    }

    // Back again only to LINECURVES because we hate grasshopper and its dealings with rhinocommon
    public static LineCurve ToNative( this SpeckleLine line )
    {
      var pts = line.Value.ToPoints();
      var myLine = new LineCurve( pts[ 0 ], pts[ 1 ] );
      if ( line.Domain != null )
        myLine.Domain = line.Domain.ToNative();
      myLine.UserDictionary.ReplaceContentsWith( line.Properties.ToNative() );
      return myLine;
    }

    // Rectangles now and forever forward will become polylines
    public static SpecklePolyline ToSpeckle( this Rectangle3d rect )
    {
      return new SpecklePolyline( ( new Point3d[ ] { rect.Corner( 0 ), rect.Corner( 1 ), rect.Corner( 2 ), rect.Corner( 3 ) } ).ToFlatArray() ) { Closed = true };
    }

    // Circle
    // Gh Capture
    public static SpeckleCircle ToSpeckle( this Circle circ )
    {
      var circle = new SpeckleCircle( circ.Plane.ToSpeckle(), circ.Radius );
      return circle;
    }

    public static ArcCurve ToNative( this SpeckleCircle circ )
    {
      Circle circle = new Circle( circ.Plane.ToNative(), ( double ) circ.Radius );

      var myCircle = new ArcCurve( circle );
      if ( circ.Domain != null )
        myCircle.Domain = circ.Domain.ToNative();
      myCircle.UserDictionary.ReplaceContentsWith( circ.Properties.ToNative() );

      return myCircle;
    }

    // Arc
    // Rh Capture can be a circle OR an arc
    public static SpeckleObject ToSpeckle( this ArcCurve a )
    {
      if ( a.IsClosed )
      {
        Circle preCircle;
        a.TryGetCircle( out preCircle );
        SpeckleCircle myCircle = preCircle.ToSpeckle();
        myCircle.Domain = a.Domain.ToSpeckle();
        myCircle.Properties = a.UserDictionary.ToSpeckle( root: a );
        myCircle.GenerateHash();
        return myCircle;
      }
      else
      {
        Arc preArc;
        a.TryGetArc( out preArc );
        SpeckleArc myArc = preArc.ToSpeckle();
        myArc.Domain = a.Domain.ToSpeckle();
        myArc.Properties = a.UserDictionary.ToSpeckle( root: a );
        myArc.GenerateHash();
        return myArc;
      }
    }

    // Gh Capture
    public static SpeckleArc ToSpeckle( this Arc a )
    {
      SpeckleArc arc = new SpeckleArc( a.Plane.ToSpeckle(), a.Radius, a.StartAngle, a.EndAngle, a.Angle );
      arc.EndPoint = a.EndPoint.ToSpeckle();
      arc.StartPoint = a.StartPoint.ToSpeckle();
      arc.MidPoint = a.MidPoint.ToSpeckle();
      return arc;
    }

    public static ArcCurve ToNative( this SpeckleArc a )
    {
      Arc arc = new Arc( a.Plane.ToNative(), ( double ) a.Radius, ( double ) a.AngleRadians );
      arc.StartAngle = ( double ) a.StartAngle;
      arc.EndAngle = ( double ) a.EndAngle;
      var myArc = new ArcCurve( arc );
      if ( a.Domain != null )
        myArc.Domain = a.Domain.ToNative();
      myArc.UserDictionary.ReplaceContentsWith( a.Properties.ToNative() );
      return myArc;
    }

    //Ellipse
    public static SpeckleEllipse ToSpeckle( this Ellipse e )
    {
      return new SpeckleEllipse( e.Plane.ToSpeckle(), e.Radius1, e.Radius2 );
    }

    public static NurbsCurve ToNative( this SpeckleEllipse e )
    {
      Ellipse elp = new Ellipse( e.Plane.ToNative(), ( double ) e.FirstRadius, ( double ) e.SecondRadius );


      var myEllp = NurbsCurve.CreateFromEllipse( elp );
      var shit = myEllp.IsEllipse( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance );

      if ( e.Domain != null )
        myEllp.Domain = e.Domain.ToNative();
      myEllp.UserDictionary.ReplaceContentsWith( e.Properties.ToNative() );

      return myEllp;
    }

    // Polyline

    // Gh Capture
    public static SpeckleObject ToSpeckle( this Polyline poly )
    {
      if ( poly.Count == 2 )
        return new SpeckleLine( poly.ToFlatArray() );

      var myPoly = new SpecklePolyline( poly.ToFlatArray() );
      myPoly.Closed = poly.IsClosed;

      if ( myPoly.Closed )
        myPoly.Value.RemoveRange( myPoly.Value.Count - 3, 3 );

      return myPoly;
    }

    // Rh Capture
    public static SpeckleObject ToSpeckle( this PolylineCurve poly )
    {
      Polyline polyline;

      if ( poly.TryGetPolyline( out polyline ) )
      {
        if ( polyline.Count == 2 )
          return new SpeckleLine( polyline.ToFlatArray(), null, poly.UserDictionary.ToSpeckle( root: poly ) );

        var myPoly = new SpecklePolyline( polyline.ToFlatArray() );
        myPoly.Closed = polyline.IsClosed;

        if ( myPoly.Closed )
          myPoly.Value.RemoveRange( myPoly.Value.Count - 3, 3 );

        myPoly.Domain = poly.Domain.ToSpeckle();
        myPoly.Properties = poly.UserDictionary.ToSpeckle( root: poly );
        myPoly.GenerateHash();
        return myPoly;
      }
      return null;
    }

    // Deserialise
    public static PolylineCurve ToNative( this SpecklePolyline poly )
    {
      var points = poly.Value.ToPoints().ToList();
      if ( poly.Closed ) points.Add( points[ 0 ] );

      var myPoly = new PolylineCurve( points );
      if ( poly.Domain != null )
        myPoly.Domain = poly.Domain.ToNative();
      myPoly.UserDictionary.ReplaceContentsWith( poly.Properties.ToNative() );
      return myPoly;
    }

    // Polycurve
    // Rh Capture/Gh Capture
    public static SpecklePolycurve ToSpeckle( this PolyCurve p )
    {
      SpecklePolycurve myPoly = new SpecklePolycurve();
      myPoly.Closed = p.IsClosed;
      myPoly.Domain = p.Domain.ToSpeckle();

      var segments = new List<Curve>();
      CurveSegments( segments, p, true );

      myPoly.Segments = segments.Select( s => { return SpeckleCore.Converter.Serialise( s ) as SpeckleObject; } ).ToList();

      myPoly.Properties = p.UserDictionary.ToSpeckle( root: p );
      myPoly.GenerateHash();

      return myPoly;
    }

    public static PolyCurve ToNative( this SpecklePolycurve p )
    {
      PolyCurve myPolyc = new PolyCurve();
      foreach ( var segment in p.Segments )
      {
        try
        {
          myPolyc.AppendSegment( ( Curve ) Converter.Deserialise( segment ) );
        }
        catch { }
      }

      myPolyc.UserDictionary.ReplaceContentsWith( p.Properties.ToNative() );
      if ( p.Domain != null )
        myPolyc.Domain = p.Domain.ToNative();
      return myPolyc;
    }

    // Curve
    public static SpeckleObject ToSpeckle( this NurbsCurve curve )
    {
      var properties = curve.UserDictionary.ToSpeckle( root: curve );

      if ( curve.IsArc( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) )
      {
        Arc getObj; curve.TryGetArc( out getObj );
        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsCircle( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) && curve.IsClosed )
      {
        Circle getObj; curve.TryGetCircle( out getObj );
        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsEllipse( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) && curve.IsClosed )
      {
        Ellipse getObj; curve.TryGetEllipse( out getObj );
        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsLinear( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) || curve.IsPolyline() ) // defaults to polyline
      {
        Polyline getObj; curve.TryGetPolyline( out getObj );
        if (null != getObj)
        {
          SpeckleObject myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
          return myObject;
        }
      }

      Polyline poly;
      curve.ToPolyline( 0, 1, 0, 0, 0, 0.1, 0, 0, true ).TryGetPolyline( out poly );

      SpecklePolyline displayValue;

      if ( poly.Count == 2 )
      {
        displayValue = new SpecklePolyline();
        displayValue.Value = new List<double> { poly[ 0 ].X, poly[ 0 ].Y, poly[ 0 ].Z, poly[ 1 ].X, poly[ 1 ].Y, poly[ 1 ].Z };
        displayValue.GenerateHash();
      }
      else
      {
        displayValue = poly.ToSpeckle() as SpecklePolyline;
      }

      SpeckleCurve myCurve = new SpeckleCurve( displayValue );
      NurbsCurve nurbsCurve = curve.ToNurbsCurve();

      myCurve.Weights = nurbsCurve.Points.Select( ctp => ctp.Weight ).ToList();
      myCurve.Points = nurbsCurve.Points.Select( ctp => ctp.Location ).ToFlatArray().ToList();
      myCurve.Knots = nurbsCurve.Knots.ToList();
      myCurve.Degree = nurbsCurve.Degree;
      myCurve.Periodic = nurbsCurve.IsPeriodic;
      myCurve.Rational = nurbsCurve.IsRational;
      myCurve.Domain = nurbsCurve.Domain.ToSpeckle();
      myCurve.Closed = nurbsCurve.IsClosed;

      myCurve.Properties = properties;
      myCurve.GenerateHash();

      return myCurve;
    }

    public static NurbsCurve ToNative( this SpeckleCurve curve )
    {
      var ptsList = curve.Points.ToPoints();

      var nurbsCurve = NurbsCurve.Create( false, curve.Degree, ptsList );

      for ( int j = 0; j < nurbsCurve.Points.Count; j++ )
      {
        nurbsCurve.Points.SetPoint( j, ptsList[ j ], curve.Weights[ j ] );
      }

      for ( int j = 0; j < nurbsCurve.Knots.Count; j++ )
      {
        nurbsCurve.Knots[ j ] = curve.Knots[ j ];
      }

      nurbsCurve.Domain = curve.Domain.ToNative();
      return nurbsCurve;
    }

    // Box
    public static SpeckleBox ToSpeckle( this Box box )
    {
      var speckleBox = new SpeckleBox( box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle() );
      speckleBox.GenerateHash();
      return speckleBox;
    }

    public static Box ToNative( this SpeckleBox box )
    {
      return new Box( box.BasePlane.ToNative(), box.XSize.ToNative(), box.YSize.ToNative(), box.ZSize.ToNative() );
    }

    // Meshes
    public static SpeckleMesh ToSpeckle( this Mesh mesh )
    {
      var verts = mesh.Vertices.ToPoint3dArray().ToFlatArray();

      //var tex_coords = mesh.TextureCoordinates.Select( pt => pt ).ToFlatArray();

      var Faces = mesh.Faces.SelectMany( face =>
      {
        if ( face.IsQuad ) return new int[ ] { 1, face.A, face.B, face.C, face.D };
        return new int[ ] { 0, face.A, face.B, face.C };
      } ).ToArray();

      var Colors = mesh.VertexColors.Select( cl => cl.ToArgb() ).ToArray();

      return new SpeckleMesh( verts, Faces, Colors, null, properties: mesh.UserDictionary.ToSpeckle( root: mesh ) );
    }

    public static Mesh ToNative( this SpeckleMesh mesh )
    {
      Mesh m = new Mesh();
      m.Vertices.AddVertices( mesh.Vertices.ToPoints() );

      int i = 0;

      while ( i < mesh.Faces.Count )
      {
        if ( mesh.Faces[ i ] == 0 )
        { // triangle
          m.Faces.AddFace( new MeshFace( mesh.Faces[ i + 1 ], mesh.Faces[ i + 2 ], mesh.Faces[ i + 3 ] ) );
          i += 4;
        }
        else
        { // quad
          m.Faces.AddFace( new MeshFace( mesh.Faces[ i + 1 ], mesh.Faces[ i + 2 ], mesh.Faces[ i + 3 ], mesh.Faces[ i + 4 ] ) );
          i += 5;
        }
      }
      try
      {
        m.VertexColors.AppendColors( mesh.Colors.Select( c => System.Drawing.Color.FromArgb( (int) c ) ).ToArray() );
      }
      catch { }

      if ( mesh.TextureCoordinates != null )
        for ( int j = 0; j < mesh.TextureCoordinates.Count; j += 2 )
        {
          m.TextureCoordinates.Add( mesh.TextureCoordinates[ j ], mesh.TextureCoordinates[ j + 1 ] );
        }

      m.UserDictionary.ReplaceContentsWith( mesh.Properties.ToNative() );
      return m;
    }

    // Breps
    public static SpeckleBrep ToSpeckle( this Brep brep )
    {
      var joinedMesh = new Mesh();

      MeshingParameters mySettings;
      mySettings = new MeshingParameters( 0 );

      Mesh.CreateFromBrep( brep, mySettings ).All( meshPart => { joinedMesh.Append( meshPart ); return true; } );


      return new SpeckleBrep( displayValue: joinedMesh.ToSpeckle(), rawData: JsonConvert.SerializeObject( brep ), provenance: "Rhino", properties: brep.UserDictionary.ToSpeckle( root: brep ) );
    }

    public static Brep ToNative( this SpeckleBrep brep )
    {
      try
      {
        if ( brep.Provenance == "Rhino" )
        {
          var myBrep = JsonConvert.DeserializeObject<Brep>( ( string ) brep.RawData );
          myBrep.UserDictionary.ReplaceContentsWith( brep.Properties.ToNative() );
          return myBrep;
        }
        throw new Exception( "Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other." );
      }
      catch
      {
        System.Diagnostics.Debug.WriteLine( "Failed to deserialise brep" );
        return null;
      }
    }

    // Extrusions
    // TODO: Research into how to properly create and recreate extrusions. Current way we compromise by transforming them into breps.
    public static SpeckleBrep ToSpeckle( this Rhino.Geometry.Extrusion extrusion )
    {
      return extrusion.ToBrep().ToSpeckle();

      //var myExtrusion = new SpeckleExtrusion( SpeckleCore.Converter.Serialise( extrusion.Profile3d( 0, 0 ) ), extrusion.PathStart.DistanceTo( extrusion.PathEnd ), extrusion.IsCappedAtBottom );

      //myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
      //myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();
      //myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();

      //var Profiles = new List<SpeckleObject>();
      //for ( int i = 0; i < extrusion.ProfileCount; i++ )
      //  Profiles.Add( SpeckleCore.Converter.Serialise( extrusion.Profile3d( i, 0 ) ) );

      //myExtrusion.Profiles = Profiles;
      //myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle( root: extrusion );
      //myExtrusion.GenerateHash();
      //return myExtrusion;
    }

    // TODO: See above. We're no longer creating new extrusions. This is here just for backwards compatibility.
    public static Rhino.Geometry.Extrusion ToNative( this SpeckleExtrusion extrusion )
    {
      Curve outerProfile = ( Curve ) Converter.Deserialise( extrusion.Profile );
      Curve innerProfile = null;

      if ( extrusion.Profiles.Count == 2 ) innerProfile = ( Curve ) Converter.Deserialise( extrusion.Profiles[ 1 ] );

      try
      {
        var IsClosed = extrusion.Profile.GetType().GetProperty( "IsClosed" ).GetValue( extrusion.Profile, null ) as bool?;
        if ( IsClosed != true )
          outerProfile.Reverse();
      }
      catch { }

      var myExtrusion = Extrusion.Create( outerProfile.ToNurbsCurve(), ( double ) extrusion.Length, ( bool ) extrusion.Capped );
      if ( innerProfile != null )
        myExtrusion.AddInnerProfile( innerProfile );

      return myExtrusion;
    }

      //  Curve profile = null;
      //  try
      //  {
      //    var toNativeMethod = extrusion.Profile.GetType().GetMethod( "ToNative" );
      //    profile = ( Curve ) toNativeMethod.Invoke( extrusion.Profile, new object[ ] { extrusion.Profile } );
      //    if ( new string[ ] { "Polyline", "Polycurve" }.Contains( extrusion.Profile.Type ) )
      //      try
      //      {
      //        var IsClosed = extrusion.Profile.GetType().GetProperty( "IsClosed" ).GetValue( extrusion.Profile, null ) as bool?;
      //        if ( IsClosed != true )
      //        {
      //          profile.Reverse();
      //        }
      //      }
      //      catch { }


      //    //switch ( extrusion.Profile )
      //    //{
      //    //  case SpeckleCore.SpeckleCurve curve:
      //    //    profile = curve.ToNative();
      //    //    break;
      //    //  case SpeckleCore.SpecklePolycurve polycurve:
      //    //    profile = polycurve.ToNative();
      //    //    if ( !profile.IsClosed )
      //    //      profile.Reverse();
      //    //    break;
      //    //  case SpeckleCore.SpecklePolyline polyline:
      //    //    profile = polyline.ToNative();
      //    //    if ( !profile.IsClosed )
      //    //      profile.Reverse();
      //    //    break;
      //    //  case SpeckleCore.SpeckleArc arc:
      //    //    profile = arc.ToNative();
      //    //    break;
      //    //  case SpeckleCore.SpeckleCircle circle:
      //    //    profile = circle.ToNative();
      //    //    break;
      //    //  case SpeckleCore.SpeckleEllipse ellipse:
      //    //    profile = ellipse.ToNative();
      //    //    break;
      //    //  case SpeckleCore.SpeckleLine line:
      //    //    profile = line.ToNative();
      //    //    break;
      //    //  default:
      //    //    profile = null;
      //    //    break;
      //    //}
      //  }
      //  catch { }
      //  var x = new Extrusion();

      //  if ( profile == null ) return null;

      //  var myExtrusion = Extrusion.Create( profile.ToNurbsCurve(), ( double ) extrusion.Length, ( bool ) extrusion.Capped );

      //  myExtrusion.UserDictionary.ReplaceContentsWith( extrusion.Properties.ToNative() );
      //  return myExtrusion;
      //}

      // Texts & Annotations
      public static SpeckleAnnotation ToSpeckle( this TextEntity textentity )
    {
      Rhino.DocObjects.Font font = Rhino.RhinoDoc.ActiveDoc.Fonts[ textentity.FontIndex ];

      var myAnnotation = new SpeckleAnnotation();
      myAnnotation.Text = textentity.Text;
      myAnnotation.Plane = textentity.Plane.ToSpeckle();
      myAnnotation.FontName = font.FaceName;
      myAnnotation.TextHeight = textentity.TextHeight;
      myAnnotation.Bold = font.Bold;
      myAnnotation.Italic = font.Italic;
      myAnnotation.GenerateHash();

      return myAnnotation;
    }

    public static SpeckleAnnotation ToSpeckle( this TextDot textdot )
    {
      var myAnnotation = new SpeckleAnnotation();
      myAnnotation.Text = textdot.Text;
      myAnnotation.Location = textdot.Point.ToSpeckle();
      myAnnotation.GenerateHash();

      return myAnnotation;
    }

    public static object ToNative( this SpeckleAnnotation annot )
    {
      if ( annot.Plane != null )
      {
        // TEXT ENTITIY 
        var textEntity = new TextEntity()
        {
          Text = annot.Text,
          Plane = annot.Plane.ToNative(),
          FontIndex = Rhino.RhinoDoc.ActiveDoc.Fonts.FindOrCreate( annot.FontName, ( bool ) annot.Bold, ( bool ) annot.Italic ),
          TextHeight = ( double ) annot.TextHeight
        };

        var dimStyleIndex = Rhino.RhinoDoc.ActiveDoc.DimStyles.Add( "Speckle" );
        var dimStyle = new Rhino.DocObjects.DimensionStyle
        {
          TextHeight = ( double ) annot.TextHeight,
          Font = new Rhino.DocObjects.Font( annot.FontName, Rhino.DocObjects.Font.FontWeight.Bold, Rhino.DocObjects.Font.FontStyle.Italic, false, false )
        };

        Rhino.RhinoDoc.ActiveDoc.DimStyles.Modify( dimStyle, dimStyleIndex, true );
        textEntity.DimensionStyleId = Rhino.RhinoDoc.ActiveDoc.DimStyles[ dimStyleIndex ].Id;
        return textEntity;
      }
      else
      {
        // TEXT DOT!
        var myTextdot = new TextDot( annot.Text, annot.Location.ToNative().Location );
        myTextdot.UserDictionary.ReplaceContentsWith( annot.Properties.ToNative() );
        return myTextdot;
      }
    }


    // Proper explosion of polycurves:
    // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon 
    public static bool CurveSegments( List<Curve> L, Curve crv, bool recursive )
    {
      if ( crv == null ) { return false; }

      PolyCurve polycurve = crv as PolyCurve;
      if ( polycurve != null )
      {
        if ( recursive ) { polycurve.RemoveNesting(); }

        Curve[ ] segments = polycurve.Explode();

        if ( segments == null ) { return false; }
        if ( segments.Length == 0 ) { return false; }

        if ( recursive )
        {
          foreach ( Curve S in segments )
          {
            CurveSegments( L, S, recursive );
          }
        }
        else
        {
          foreach ( Curve S in segments )
          {
            L.Add( S.DuplicateShallow() as Curve );
          }
        }

        return true;
      }

      //Nothing else worked, lets assume it's a nurbs curve and go from there...
      NurbsCurve nurbs = crv.ToNurbsCurve();
      if ( nurbs == null ) { return false; }

      double t0 = nurbs.Domain.Min;
      double t1 = nurbs.Domain.Max;
      double t;

      int LN = L.Count;

      do
      {
        if ( !nurbs.GetNextDiscontinuity( Continuity.C1_locus_continuous, t0, t1, out t ) ) { break; }

        Interval trim = new Interval( t0, t );
        if ( trim.Length < 1e-10 )
        {
          t0 = t;
          continue;
        }

        Curve M = nurbs.DuplicateCurve();
        M = M.Trim( trim );
        if ( M.IsValid ) { L.Add( M ); }

        t0 = t;
      } while ( true );

      if ( L.Count == LN ) { L.Add( nurbs ); }

      return true;
    }

  }
}
