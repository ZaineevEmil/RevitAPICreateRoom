using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Планиг создает помещения и присваивает им имена на планах этажей, созданных в проекте Revit
namespace RevitAPICreateRoom
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class RevitAPICreateRoom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //собираем все уровни в проекте
                List<Level> levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .OfType<Level>()
                    .ToList();
                if (levels == null)
                {
                    return Result.Cancelled;
                }

                //находим фазу проекта к которой будут относится помещения
                Phase phase = new FilteredElementCollector(doc)
                    .OfClass(typeof(Phase))
                    .OfType<Phase>()
                    .Where(x => x.Name.Equals("Стадия 1"))
                    .FirstOrDefault();

                using (var ts = new Transaction(doc, "Создание комнат"))
                {
                    ts.Start();
                    //создаем переменную для записи всех RoomID
                    List<ElementId> roomsID = null;

                    //метод для создания помещений на планах этажей, созданных в проекте Revit
                    foreach (Level level in levels)
                    {
                        ElementId planViewId = null;
                        planViewId = level.FindAssociatedPlanViewId();
                        if (planViewId.IntegerValue != -1)
                        {
                            List<ElementId> roomsLevelID = doc.Create.NewRooms2(level, phase) as List<ElementId>;
                            if (roomsID == null)
                            {
                                roomsID = roomsLevelID;
                            }
                            else
                            {
                                roomsID.AddRange(roomsLevelID);
                            }
                        }
                    }

                    //создаем метод для внесения изменений в метки помещений
                    foreach (ElementId roomID in roomsID)
                    {
                        Room room = doc.GetElement(roomID) as Room;
                        room.Number = room.Level.Name.Remove(0, 8) + "_" + room.Number;
                    }
                    ts.Commit();
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }


        public Level GetLevel(List<Level> levels, string nameLevel)
        {
            Level level1 = levels
                .Where(x => x.Name.Equals(nameLevel))
                .FirstOrDefault();
            return level1;
        }
    }
}
