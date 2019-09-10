/// <reference path="three.min.js" />
String.prototype.contains = function (it) { return this.indexOf(it) != -1; };

// Create a list to keep all the models in
var models = new Array();

function importObj(text, scene)
{
    // TODO: implement this
}

function importBinaryStl(file, scene)
{
    console.log("importing binary stl");

    var reader = new FileReader();
    reader.onload = function (e) {
        var buf = e.target.result;
        //var bytes = new Uint8Array(buf, 0, buf.byteLength);
        //console.log(bytes);
        var faceCount = new Uint32Array(buf, 80, 1)[0];
        //console.log("Faces: " + faceCount);
        //var floats = new Float32Array(buf, 84, faceCount);
        
        var geometry = new THREE.Geometry();

        var bufIdx = 84;
        var vertIdx = -1;

        //alert("facecount: " + faceCount);
        //alert("bytelength: " + buf.byteLength);
        //alert("estsize: " + (faceCount * 50) + 80);

        //return;

        for (var i = 0; i < faceCount; i++)
        {
            try
            {
                var floats = new Float32Array(buf, bufIdx, 12);
                var normals = new THREE.Vector3(floats[0], floats[1], floats[2]);
                geometry.vertices.push(new THREE.Vector3(floats[3], floats[4], floats[5]));
                geometry.vertices.push(new THREE.Vector3(floats[6], floats[7], floats[8]));
                geometry.vertices.push(new THREE.Vector3(floats[9], floats[10], floats[11]));
                geometry.faces.push(new THREE.Face3(vertIdx++, vertIdx++, vertIdx++, normals));
                bufIdx += 50;
                //vertIdx += 3;
            }
            catch (err)
            {
                alert(bufIdx);
                alert(buf.byteLength);
                alert(err);
                return;
            }
            
        }

        var model = new THREE.Mesh(geometry, new THREE.MeshNormalMaterial());
        scene.add(model);
    }
    reader.readAsArrayBuffer(file);
}

function importAsciiStl(text, scene)
{
    //TODO: implement this
}

function importStl(text, file, scene)
{
    if (text.contains("face"))
        importAsciiStl(text, scene);
    else
        importBinaryStl(file, scene);
}

function importModel(file, scene) {
    console.log(file.name);
    var reader = new FileReader();
    reader.onload = function (e) {
        if (file.name.contains(".stl")) {
            importStl(e.target.result, file, scene);
            alert('importing stl');
        }
        else if (file.name.contains(".obj"))
            importObj(e.target.result, scene);
        else
            alert("Only .stl and .obj files are supported at the moment");
    }
    reader.readAsText(file);
}