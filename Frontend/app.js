"use strict";

let disableRendering = false;
let jsonOutputTextArea;
$(() => {

    const textAreas = Array.from(document.getElementsByTagName("textarea"));
    const textAreasWithoutJson = textAreas.filter(ta => ta.id != "JsonOutput");
    jsonOutputTextArea = document.getElementById("JsonOutput");
    textAreasWithoutJson.forEach(textArea => {
        let previousTextLines = 0;
        textArea.oninput = () => {
            const currentTextLines = getListFromTextArea(textArea).length;
            if (currentTextLines === previousTextLines) {
                return;
            }
            previousTextLines = currentTextLines;
            setTextAreaHeight(textArea);
            inputToJson();
            setAllPersonOptions();
        }
        setTextAreaHeight(textArea);
    });

    jsonOutputTextArea.oninput = () => {
        setTextAreaHeight(jsonOutputTextArea);
        jsonToInput();
    };
    setTextAreaHeight(jsonOutputTextArea);

    const inputs = Array.from(document.getElementsByTagName("input"));
    inputs.forEach(textarea => textarea.oninput = () => inputToJson());

    jsonToInput();
});

function togglePanel(id) {
    $("#" + id).slideToggle(500);
}

function addNewGroupAssociationManually(elementOnTop) {
    const newRow = addNewGroupAssociation();
    newRow.scrollIntoView({behavior: "smooth", block: "center"});
}

function addNewGroupAssociation() {
    const associationsTable = document.getElementById("GroupAssociationsTable");
    const newRow = document.createElement("tr");
    newRow.classList.add("groupAssociation");
    newRow.id = "groupAssociation" + document.getElementsByClassName("groupAssociation").length;
    associationsTable.appendChild(newRow);

    const fromPersonCell = document.createElement("td");
    fromPersonCell.classList.add("associationWrapper");
    newRow.appendChild(fromPersonCell);
    const fromPersonSelect = document.createElement("select");
    fromPersonSelect.classList.add("fromPerson");
    fromPersonCell.appendChild(fromPersonSelect);

    const toPersonsCell = document.createElement("td");
    newRow.appendChild(toPersonsCell);
    const toPersonsSelect = document.createElement("select");
    toPersonsSelect.classList.add("toPersons");
    toPersonsCell.appendChild(toPersonsSelect);

    var fromPersonJQuery = $(fromPersonSelect);
    fromPersonJQuery.selectize({
        options: [getUnselectableOption()],
        maxItems: 1,
        closeAfterSelect: true,
        valueField: 'name',
        labelField: 'name',
        searchField: 'name',
        onChange: () => onSelectizeChange(toPersonsSelect)
    });

    const toPersonInputJQuery = $(toPersonsSelect);
    toPersonInputJQuery.selectize({
        options: [getUnselectableOption()],
        maxItems: null,
        valueField: 'name',
        labelField: 'name',
        searchField: 'name',
        onChange: () => onSelectizeChange(fromPersonSelect),
        plugins: ["remove_button"]
    });

    const weightCell = document.createElement("td");
    newRow.appendChild(weightCell);
    const weightInput = document.createElement("input");
    weightInput.type = "number";
    weightInput.step = 1;
    weightInput.value = getLastWeight();
    weightInput.classList.add("weight");
    weightInput.oninput = () => inputToJson();
    weightCell.appendChild(weightInput);

    const removeCell = document.createElement("td");
    newRow.appendChild(removeCell);
    const removeButton = document.createElement("button");
    removeButton.textContent = "Löschen";
    removeButton.onclick = () => {
        associationsTable.removeChild(newRow);
        inputToJson();
    };
    removeCell.appendChild(removeButton);

    setAllPersonOptions([fromPersonSelect, toPersonsSelect])
    return newRow;
}

function onSelectizeChange(otherElement) {
    inputToJson();
    setAllPersonOptions([otherElement]);
}

function setTextAreaHeight(textArea) {
    textArea.style.height = ""; /* Reset the height*/
    textArea.style.height = textArea.scrollHeight + "px";
}

function getLastWeight() {
    const otherWeightInputs = document.getElementsByClassName("weight");
    if (otherWeightInputs.length < 1) {
        return 0;
    }

    return otherWeightInputs[otherWeightInputs.length - 1].value;
}

function setAllPersonOptions(onlyElements) {
    const allFromPersons = Array.from(document.getElementsByClassName("fromPerson"))
        .filter(e => e.tagName === "SELECT" && (!onlyElements || onlyElements.includes(e)));
    const allToPersons = Array.from(document.getElementsByClassName("toPersons"))
        .filter(e => e.tagName === "SELECT" && (!onlyElements || onlyElements.includes(e)));

    allFromPersons.forEach(input => setPersonOptions($(input), false));
    allToPersons.forEach(input => setPersonOptions($(input), true));
}

function setPersonOptions(selectJQueryElement, multiple) {
    if (disableRendering) {
        return;
    }

    const selectize = selectJQueryElement[0].selectize;
    let selectedOptions = selectize.getValue();
    selectize.clearOptions(true);
    let allOptions = getAllPersons();

    const associationTr = selectJQueryElement.parent().parent()
    if (multiple) {
        const fromPerson = associationTr.find(".fromPerson")[0].selectize.getValue();
        allOptions = allOptions.filter(p => p !== fromPerson);
    } else {
        const toPersons = associationTr.find(".toPersons")[0].selectize.getValue();
        allOptions = allOptions.filter(p => !toPersons.includes(p));
    }

    allOptions.sort();
    if (!allOptions.length) {
        selectize.addOption(getUnselectableOption());
    }
    allOptions.forEach(p => selectize.addOption({ name: p }));

    disableRendering = true;
    try {
        //Disable rendering, otherwise this setvalue would fire change and setPersonOptions of other select
        //Then the other select would fire change setPersonOptions of this select
        //and we have a recursive loop
        if (multiple && selectedOptions && selectedOptions.length && allOptions.length) {
            selectedOptions = selectedOptions.filter(p => allOptions.includes(p));
            selectize.setValue(selectedOptions);
        } else if (!multiple && selectedOptions && allOptions.includes(selectedOptions)) {
            selectize.setValue(selectedOptions);
        }
    } finally {
        disableRendering = false;
    }

    selectize.refreshOptions(false);
}

function getAllPersons() {
    const memberLists = getMemberListTextAreas();
    let allOptions = [];
    memberLists.forEach(memberList => getListFromTextArea(memberList).forEach(p => {
        if (!allOptions.includes(p)) {
            allOptions.push(p);
        }
    }));
    return allOptions;
}

function getUnselectableOption() {
    return { name: "Kein Eintrag vorhanden", disabled: true};
}

function getMemberListTextAreas() {
    return Array.from(document.getElementById("PeopleLists").getElementsByTagName("textarea"));
}

function copyJsonToClipBoard() {
    jsonOutputTextArea.select();
    jsonOutputTextArea.setSelectionRange(0, 99999); // For mobile devices
    // Copy the text inside the text field
    navigator.clipboard.writeText(jsonOutputTextArea.value);
}

function jsonToInput() {
    if (disableRendering === true || !jsonOutputTextArea.value) {
        return;
    }

    disableRendering = true;

    try {
        const jsonObject = JSON.parse(jsonOutputTextArea.value);
        const inputs = Array.from(document.getElementById("SingleValues").getElementsByTagName("input"));
        inputs.forEach(input => {
            const jsonObjectValue = jsonObject[input.id];
            input.value = getInputFromJsonSingleValue(jsonObjectValue);
        });

        const memberLists = getMemberListTextAreas();
        memberLists.forEach(memberList => {
            const jsonObjectValue = jsonObject[memberList.id];
            if (jsonObjectValue && jsonObjectValue.length) {
                memberList.value = jsonObjectValue.filter(p => p).join("\n");
            } else {
                memberList.value = "";
            }
        });

        const associations = Array.from(document.getElementsByClassName("groupAssociation"));
        associations.forEach(a => a.remove());

        const allPersons = getAllPersons();

        if (jsonObject.OneToManyAssociations && jsonObject.OneToManyAssociations) {
            jsonObject.OneToManyAssociations.forEach(a => {
                const row = addNewGroupAssociation();

                const fromPersonSelectize = $(row.getElementsByClassName("fromPerson")[0])[0].selectize;
                allPersons.forEach(p => fromPersonSelectize.addOption({ name: p }));
                fromPersonSelectize.setValue(a.FromPerson);

                const toPersonsSelectize = $(row.getElementsByClassName("toPersons")[0])[0].selectize;
                allPersons.forEach(p => toPersonsSelectize.addOption({ name: p }));
                toPersonsSelectize.setValue(a.ToPersons ?? []);

                row.getElementsByClassName("weight")[0].value = getInputFromJsonSingleValue(a.Weight);
            });
        }

        if (jsonObject.ManyAssociations && jsonObject.ManyAssociations) {
            jsonObject.ManyAssociations.forEach(a => {
                if (!a.People) {
                    a.People = [];
                }
                const row = addNewGroupAssociation();

                const fromPersonSelectize = $(row.getElementsByClassName("fromPerson")[0])[0].selectize;
                allPersons.forEach(p => fromPersonSelectize.addOption({ name: p }));
                fromPersonSelectize.setValue(a.People[0]);

                const toPersonsSelectize = $(row.getElementsByClassName("toPersons")[0])[0].selectize;
                allPersons.forEach(p => toPersonsSelectize.addOption({ name: p }));
                toPersonsSelectize.setValue(a.People.slice(1));

                row.getElementsByClassName("weight")[0].value = getInputFromJsonSingleValue(a.Weight);
            });
        }

    } finally {
        disableRendering = false;
    }

    //Set all options right
    setAllPersonOptions();

    getMemberListTextAreas().forEach(ta => setTextAreaHeight(ta));
}

function getInputFromJsonSingleValue(intValue) {
    if (intValue !== null && intValue !== undefined) {
        return intValue;
    } else {
        return "";
    }
}

function inputToJson() {
    if (disableRendering === true) {
        return;
    }

    disableRendering = true;

    try {
        const inputs = Array.from(document.getElementById("SingleValues").getElementsByTagName("input"));
        const resultingObject = {};
        inputs.forEach(input => {
            resultingObject[input.id] = getIntFromInput(input);
        });

        const memberLists = getMemberListTextAreas();
        memberLists.forEach(memberList => {
            resultingObject[memberList.id] = getListFromTextArea(memberList, true);
        });

        resultingObject.OneToManyAssociations = [];
        const associations = Array.from(document.getElementsByClassName("groupAssociation"));
        associations.forEach(associationRow => {
            const association = {};
            const fromPerson = associationRow.getElementsByClassName("fromPerson")[0].selectize.getValue();
            if (!fromPerson) {
                return;
            }
            association.FromPerson = fromPerson;

            const toPersons = associationRow.getElementsByClassName("toPersons")[0].selectize.getValue();
            if (!toPersons.length) {
                return;
            }
            association.ToPersons = toPersons;

            const weightInput = associationRow.getElementsByClassName("weight")[0];
            association.Weight = getIntFromInput(weightInput);
            resultingObject.OneToManyAssociations.push(association);
        });

        jsonOutputTextArea.value = JSON.stringify(resultingObject, null, 4);
        setTextAreaHeight(jsonOutputTextArea);
    } finally {
        disableRendering = false;
    }

}

function getListFromTextArea(textArea, doFilter) {
    let resultingArray = [];
    textArea.value.split("\r\n")
        .forEach(v => v.split("\n").forEach(v2 => resultingArray.push(v2)));
    if (doFilter) {
        resultingArray = resultingArray.filter(v => v !== "");
    }
    return resultingArray;
}

function getIntFromInput(input) {
    const stringValue = input.value;
    if (stringValue === null || stringValue === undefined || stringValue === "") {
        return null;
    }
    return Number.parseInt(stringValue);
}

function download() {
    var element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(jsonOutputTextArea.value));
    element.setAttribute('download', "input.json");

    element.style.display = 'none';
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
}