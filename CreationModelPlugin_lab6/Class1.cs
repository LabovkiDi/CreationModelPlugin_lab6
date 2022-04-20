using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin_lab6
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //доступ к документу Revit
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevels(doc, "Уровень 1");
            Level level2 = GetLevels(doc, "Уровень 2");


            //ширина крыши
            double width = UnitUtils.ConvertToInternalUnits(10500, UnitTypeId.Millimeters);
            //глубина крыши
            double deepth = UnitUtils.ConvertToInternalUnits(5500, UnitTypeId.Millimeters);
            //получение набора точек
            double dx = width / 2;
            double dy = deepth / 2;

            Transaction transaction = new Transaction(doc, "Построение модели");
            transaction.Start();
            {
                //построение стен
                List<Wall> walls = CreateWalls(dx, dy, level1, level2, doc);
                //добавление двери
                AddDoor(doc, level1, walls[0]);
                //добавление окон
                AddWindow(doc, level1, walls[1]);
                AddWindow(doc, level1, walls[2]);
                AddWindow(doc, level1, walls[3]);
                AddRoof(doc, level2, walls, width,deepth);

            }
            transaction.Commit();

            return Result.Succeeded;
        }

        private void AddRoof(Document doc, Level level2, List<Wall> walls, double width, double deepth)
        {
            RoofType roofType = new FilteredElementCollector(doc)   //фильтр по типу
                     .OfClass(typeof(RoofType))
                     .OfType<RoofType>()
                     .Where(x => x.Name.Equals("Типовой - 400мм")) //фильтр по имени
                     .Where(x => x.FamilyName.Equals("Базовая крыша")) //фильтр по семейству
                     .FirstOrDefault();

            //через фильтр добираемся до нужного нам уровня
            View view = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .OfType<View>()
                    .Where(x => x.Name.Equals("Уровень 2"))
                    .FirstOrDefault();

            double wallWidth = walls[0].Width; //ширина стены
            double dt = wallWidth / 2;
                 
            //находим смещение для выдавливания
            double extrusionStart = -width / 2 - dt; 
            double extrusionEnd = width / 2 + dt;
            //находим смещение для построения кривой профиля стен
            double curveStart = -deepth / 2 - dt;
            double curveEnd = +deepth / 2 + dt;
            //создание массива кривых для профиля стены (границы дома)
            CurveArray curveArray = new CurveArray();          
            curveArray.Append(Line.CreateBound(new XYZ(0, curveStart, level2.Elevation), new XYZ(0, 0, level2.Elevation + 10)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, level2.Elevation + 10), new XYZ(0, curveEnd, level2.Elevation)));
            //создание рабочей плоскости для выдавливания крыши 
            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), view);
            //построения крыши выдавливанием
            ExtrusionRoof extrusionRoof = doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, extrusionStart, extrusionEnd);
            //добавим нашей крыше свойство, позволяющее срезать карниз под прямым углом, тем самым образовав примыкание со стенами
            extrusionRoof.EaveCuts = EaveCutterType.TwoCutSquare;
        }



        //private void AddRoof(Document doc, Level level2, List<Wall> walls)
        //{
        //    RoofType roofType = new FilteredElementCollector(doc)  //фильтр по типу
        //        .OfClass(typeof(RoofType))
        //        .OfType<RoofType>()
        //        .Where(x => x.Name.Equals("Типовой - 400мм")) //фильтр по имени
        //        .Where(x => x.FamilyName.Equals("Базовая крыша")) //фильтр по семейству
        //        .FirstOrDefault();

        //    double wallWidth = walls[0].Width; //ширина стены
        //    double dt = wallWidth / 2;
        //    //находим смещение
        //    List<XYZ> points = new List<XYZ>();
        //    points.Add(new XYZ(-dt, -dt, 0));
        //    points.Add(new XYZ(dt, -dt, 0));
        //    points.Add(new XYZ(dt, dt, 0));
        //    points.Add(new XYZ(-dt, dt, 0));
        //    points.Add(new XYZ(-dt, -dt, 0));


        //    Application application = doc.Application;
        //    CurveArray footprint = application.Create.NewCurveArray();
        //    for (int i = 0; i < 4; i++)
        //    {
        //        //получаем кривую
        //        LocationCurve curve = walls[i].Location as LocationCurve;
        //        XYZ p1 = curve.Curve.GetEndPoint(0); //начало отрезка
        //        XYZ p2 = curve.Curve.GetEndPoint(1); //конец точка
        //        //получаем линию
        //        Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
        //        //добавляем созданную линию
        //        footprint.Append(line);
        //    }
        //    //создаем пустой массив
        //    ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
        //    //создаем метод,в который передаем массив, и после вызова которого в нем окажется набор ребер крыши
        //    FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);

        //    foreach (ModelCurve m in footPrintToModelCurveMapping)
        //    {
        //        //добавление уклона крыши
        //        footPrintRoof.set_DefinesSlope(m, true);
        //        footPrintRoof.set_SlopeAngle(m, 0.5);
        //    }

        //}

        private void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 1220 мм"))   //метод расширения LINQ
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault(); //для получения единичного экземпляра

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0); //левая граница
            XYZ point2 = hostCurve.Curve.GetEndPoint(1); //правая граница
            //на основе 2ух точек, найдем точку куда будем устанавливать дверь
            XYZ point = (point1 + point2) / 2;
            if (!windowType.IsActive)
                windowType.Activate();

            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            //устанавливаем высоту над уровнем level1 для вставки окна
            //для вставки на заданную высоту используем параметр INSTANCE_SILL_HEIGHT_PARAM-высота нижнего бруса
            double windowHeight = UnitUtils.ConvertToInternalUnits(1200, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(windowHeight);
        }

        //метод добавления двери
        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))   //метод расширения LINQ
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault(); //для получения единичного экземпляра

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0); //левая граница
            XYZ point2 = hostCurve.Curve.GetEndPoint(1); //правая граница
            //на основе 2ух точек, найдем точку куда будем устанавливать дверь
            XYZ point = (point1 + point2) / 2;
            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        //метод выбора уровня
        public Level GetLevels(Document doc, string levelName)
        {
            //фильтр по уровням
            List<Level> listlevel = new FilteredElementCollector(doc)
                  .OfClass(typeof(Level))
                  .OfType<Level>()
                  .ToList();
            Level level = listlevel
                  .Where(x => x.Name.Equals(levelName))     //фильтр по имени уровня
                 .FirstOrDefault();
            return level;
        }
        //метод создания стен
        public List<Wall> CreateWalls(double dx, double dy, Level level1, Level level2, Document doc)
        {
            //массив, в который добавляем созданные стены
            List<Wall> walls = new List<Wall>();
            //коллекция с точками
            List<XYZ> points = new List<XYZ>();

            //цикл создания стен
            for (int i = 0; i < 4; i++)
            {
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));
                //создание отрезка
                Line line = Line.CreateBound(points[i], points[i + 1]);
                //построение стены по отрезку
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                //находим высоту стены, привязывая ее к уровню
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                //добавляем в список созданную стену
                walls.Add(wall);
            }
            return walls;
        }
    }
}
