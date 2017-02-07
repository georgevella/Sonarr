var Marionette = require('marionette');
var Backgrid = require('backgrid');
var QueueCollection = require('./QueueCollection');
var MovieTitleCell = require('../../Cells/MovieTitleCell');
var EpisodeNumberCell = require('../../Cells/EpisodeNumberCell');
var EpisodeTitleCell = require('../../Cells/EpisodeTitleCell');
var QualityCell = require('../../Cells/QualityCell');
var QueueStatusCell = require('./QueueStatusCell');
var QueueActionsCell = require('./QueueActionsCell');
var TimeleftCell = require('./TimeleftCell');
var ProgressCell = require('./ProgressCell');
var ProtocolCell = require('../../Release/ProtocolCell');
var GridPager = require('../../Shared/Grid/Pager');

module.exports = Marionette.Layout.extend({
    template : 'Activity/Queue/QueueLayoutTemplate',

    regions : {
        table : '#x-queue',
        pager : '#x-queue-pager'
    },

    columns : [
        {
            name      : 'status',
            label     : '',
            cell      : QueueStatusCell,
            cellValue : 'this'
        },
        {
            name     : 'item',
            label    : 'Movie / TV Show',
            cell     : MovieTitleCell,
            cellValue : 'this'
        },
        /*{
            name     : 'episode',
            label    : 'Episode',
            cell     : EpisodeNumberCell
        },
        {
            name      : 'episodeTitle',
            label     : 'Episode Title',
            cell      : EpisodeTitleCell,
            cellValue : 'episode'
        },*/
        {
            name     : 'quality',
            label    : 'Quality',
            cell     : QualityCell,
            sortable : false
        },
        {
            name  : 'protocol',
            label : 'Protocol',
            cell  : ProtocolCell
        },
        {
            name      : 'timeleft',
            label     : 'Time Left',
            cell      : TimeleftCell,
            cellValue : 'this'
        },
        {
            name      : 'sizeleft',
            label     : 'Progress',
            cell      : ProgressCell,
            cellValue : 'this'
        },
        {
            name      : 'status',
            label     : '',
            cell      : QueueActionsCell,
            cellValue : 'this'
        }
    ],

    initialize : function() {
        this.listenTo(QueueCollection, 'sync', this._showTable);
    },

    onShow : function() {
        this._showTable();
    },

    _showTable : function() {
        this.table.show(new Backgrid.Grid({
            columns    : this.columns,
            collection : QueueCollection,
            className  : 'table table-hover'
        }));

        this.pager.show(new GridPager({
            columns    : this.columns,
            collection : QueueCollection
        }));
    }
});
