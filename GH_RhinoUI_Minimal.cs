using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using Eto.Forms;
using Eto.Drawing;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(bool show, ref object A)
  {
        if (show)
    {
      if (ParamWindow.Instance == null)
      {
        var win = new ParamWindow(PARAMS);
        win.Show();
      }
    }
    else
    {
      if (ParamWindow.Instance != null)
      {
        ParamWindow.Instance.Close();
        ParamWindow.Instance = null;
      }
    }
  }

  // <Custom additional code> 
    /// <summary>
  /// These are the parameters that appear in rhino interface add/remove params here.
  /// San Fran folks : names must be same as sliders i used a class cause its cleaner and solves some clashes.
  /// </summary>
  static List<ParamDef> PARAMS = new List<ParamDef>
    {
      new ParamDef("topCircleX"),
      new ParamDef("topCircleY"),
      new ParamDef("bottomCircleX"),
      new ParamDef("bottomCircleY"),
      new ParamDef("topCircleR"),
      new ParamDef("bottomCircleR")
      };

  // Simple data holder for one parameter's definition
  public class ParamDef
  {
    public string Name;

    public ParamDef(string name)
    {
      Name = name;
    }
  }

  /// <summary>
  /// GH canvas sliders methods to search and wire
  /// </summary>

  // Searches the GH canvas for a slider by its nickname
  static GH_NumberSlider FindSlider(GH_Document doc, string nickname)
  {
    foreach (var obj in doc.Objects)
    {
      GH_NumberSlider sl = obj as GH_NumberSlider;
      if (sl != null && sl.NickName == nickname)
        return sl;
    }
    return null;
  }

  // Gets the current value of a slider
  static double? GetSliderValue(GH_Document doc, string nickname)
  {
    var sl = FindSlider(doc, nickname);
    if (sl != null)
      return (double) sl.Slider.Value;
    return null;
  }

  // Sets a slider to a new value and triggers GH to recompute
  static void SetSlider(GH_Document doc, string nickname, double value)
  {
    var sl = FindSlider(doc, nickname);
    if (sl == null) return;

    double clamped = Math.Max((double) sl.Slider.Minimum,
      Math.Min((double) sl.Slider.Maximum, value));

    sl.Slider.Value = (decimal) clamped;
    sl.ExpireSolution(false);
  }


  public class ParamWindow : Form
  {
    // Static reference keeps the window alive between GH solves
    public static ParamWindow Instance;

    private List<ParamDef> _config;

    // These store the UI controls so we can read/write them
    private Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();
    private Dictionary<string, Label>  _readouts = new Dictionary<string, Label>();

    // Prevents feedback loops when we set slider values in code
    private bool _updating = false;

    private static readonly string PresetDir =
      Path.Combine(Environment.GetFolderPath(
      Environment.SpecialFolder.UserProfile), "GH_Presets");

    public ParamWindow(List<ParamDef> config)
    {
      Instance = this;
      this.Closed += (s, e) => { Instance = null; };
      _config = config;
      Title = "Parametric Controls";
      Resizable = true;
      MinimumSize = new Size(460, 80);
      BuildUI();
    }

    void BuildUI()
    {
      var doc = Instances.ActiveCanvas.Document;

      var mainLayout = new DynamicLayout
        {
          Padding = new Padding(14),
          Spacing = new Size(0, 0)
          };

      // Grid holds one row per parameter
      var grid = new TableLayout
        {
          Spacing = new Size(10, 8),
          Padding = new Padding(0, 0, 0, 14)
          };

      foreach (var p in _config)
      {
        GH_NumberSlider ghSl = FindSlider(doc, p.Name);
        if (ghSl == null) continue;

        double ghMin = (double) ghSl.Slider.Minimum;
        double ghMax = (double) ghSl.Slider.Maximum;
        double ghCurrent = (double) ghSl.Slider.Value;
        int decimals = ghSl.Slider.DecimalPlaces;
        int scale = (int) Math.Pow(10, decimals);

        var nameLbl = new Label
          {
            Text = p.Name,
            Width = 130,
            VerticalAlignment = VerticalAlignment.Center
            };

        var slider = new Slider
          {
            MinValue = (int) (ghMin * scale),
            MaxValue = (int) (ghMax * scale),
            Value = (int) (ghCurrent * scale),
            Width = 200
            };

        var readout = new Label
          {
            Text = ghCurrent.ToString("F" + decimals),
            Width = 55,
            VerticalAlignment = VerticalAlignment.Center
            };

        _sliders[p.Name] = slider;
        _readouts[p.Name] = readout;

        // Capture loop variables for the closure
        var pCopy = p;
        var readoutCopy = readout;
        var sliderCopy = slider;

        int capturedScale = scale;
        int capturedDecimals = decimals;
        string capturedName = p.Name;

        slider.ValueChanged += (s, e) =>
          {
          if (_updating) return;

          double val = sliderCopy.Value / (double) capturedScale;
          readoutCopy.Text = val.ToString("F" + capturedDecimals);

          var d = Instances.ActiveCanvas.Document;
          SetSlider(d, capturedName, val);
          d.NewSolution(false);
          };

        grid.Rows.Add(new TableRow(
          new TableCell(nameLbl, false),
          new TableCell(slider, true),
          new TableCell(readout, false)
          ));
      }

      mainLayout.AddRow(grid);

      // Button row at the bottom
      var btnLayout = new DynamicLayout { Spacing = new Size(8, 0) };
      btnLayout.BeginHorizontal();

      var saveBtn = new Button { Text = "Save Preset" };
      var loadBtn = new Button { Text = "Load Preset" };
      var syncBtn = new Button { Text = "Sync from GH" };

      saveBtn.Click += OnSave;
      loadBtn.Click += OnLoad;
      syncBtn.Click += OnSync;

      btnLayout.Add(saveBtn);
      btnLayout.Add(loadBtn);
      btnLayout.Add(syncBtn);
      btnLayout.EndHorizontal();

      mainLayout.AddRow(btnLayout);
      Content = mainLayout;
    }

    // Sync button — pulls current GH slider values INTO the panel
    void OnSync(object sender, EventArgs e)
    {
      _updating = true;
      var doc = Instances.ActiveCanvas.Document;

      foreach (var p in _config)
      {
        double? val = GetSliderValue(doc, p.Name);
        if (val == null) continue;

        GH_NumberSlider ghSl = FindSlider(doc, p.Name);
        if (ghSl == null) continue;
        int dec = ghSl.Slider.DecimalPlaces;
        int scale = (int) Math.Pow(10, dec);
        _sliders[p.Name].Value = (int) (val.Value * scale);
        _readouts[p.Name].Text = val.Value.ToString("F" + dec);
      }

      _updating = false;
    }

    void OnSave(object sender, EventArgs e)
    {
      var doc = Instances.ActiveCanvas.Document;
      var preset = new Dictionary<string, double>();

      foreach (var p in _config)
      {
        double? v = GetSliderValue(doc, p.Name);
        if (v.HasValue) preset[p.Name] = v.Value;
      }

      var dlg = new SaveFileDialog { Title = "Save Preset" };
      dlg.Filters.Add(new FileFilter("JSON Preset", ".json"));

      if (!Directory.Exists(PresetDir))
        Directory.CreateDirectory(PresetDir);

      if (dlg.ShowDialog(this) == DialogResult.Ok)
      {
        string path = dlg.FileName;
        if (!path.EndsWith(".json")) path += ".json";
        File.WriteAllText(path,
          JsonConvert.SerializeObject(preset, Formatting.Indented));
      }
    }

    void OnLoad(object sender, EventArgs e)
    {
      var dlg = new OpenFileDialog { Title = "Load Preset" };
      dlg.Filters.Add(new FileFilter("JSON Preset", ".json"));

      if (dlg.ShowDialog(this) != DialogResult.Ok) return;

      var preset = JsonConvert.DeserializeObject<Dictionary<string, double>>(
        File.ReadAllText(dlg.FileName));

      var doc = Instances.ActiveCanvas.Document;
      _updating = true;

      foreach (var kvp in preset)
      {
        SetSlider(doc, kvp.Key, kvp.Value);

        if (_sliders.ContainsKey(kvp.Key))
        {
          GH_NumberSlider ghSl = FindSlider(doc, kvp.Key);
          if (ghSl == null) continue;
          int dec = ghSl.Slider.DecimalPlaces;
          int scale = (int) Math.Pow(10, dec);
          _sliders[kvp.Key].Value = (int) (kvp.Value * scale);
          _readouts[kvp.Key].Text = kvp.Value.ToString("F" + dec);
        }
      }

      _updating = false;
      doc.NewSolution(false);
    }
  }
  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        bool show = default(bool);
    if (inputs[0] != null)
    {
      show = (bool)(inputs[0]);
    }



    //3. Declare output parameters
      object A = null;


    //4. Invoke RunScript
    RunScript(show, ref A);
      
    try
    {
      //5. Assign output parameters to component...
            if (A != null)
      {
        if (GH_Format.TreatAsCollection(A))
        {
          IEnumerable __enum_A = (IEnumerable)(A);
          DA.SetDataList(1, __enum_A);
        }
        else
        {
          if (A is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(A));
          }
          else
          {
            //assign direct
            DA.SetData(1, A);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}