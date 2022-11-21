

const Accounts = {
    template: '' +
        '<div> <h3> Список клиентских счетов </h3>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<div>Код счета: '+
                    '<input v-model="account_selected" /><button v-on:click="viewAccount">Ok</button>' +
                '</div > ' +
                '<p>' +
                    '<router-link v-for="p in pageRefs" :to="p">{{ p.params.page }}&nbsp&nbsp&nbsp</router-link>' +
                '</p>' +
                '<ul>' +
                    '<li v-for="c in accountRefs">' +
                        '<router-link :to="c">{{ c.params.name }}</router-link>' +
                    '</li >' +
                '</ul>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div>',
    data() {
        return {
            accounts: [],
            pages: 0,
            account_selected: '',
            isViewReady: false
        };
    },
    watch: { '$route': 'refreshData' },
    computed: {
        pageRefs() {
            var refs = [];
            for (var i = 1; i <= this.pages; i++)
                refs.push({ name: 'accounts', params: { page: i } });
            return refs;
        },
        accountRefs()
        {
            var refs = [];
            for(const c of this.accounts)
                refs.push({ name: 'account', params: { id: c.id, name: c.name } });
            return refs;
        }
    },
    methods: {
        viewAccount: function(event)
            {
                axios.get('/api/info/acode/' + this.account_selected)
                    .then(response => {
                        if (response.data.id > 0)
                        {
                            router.push({ name: 'account', params: { id: response.data.id, name: response.data.name } });
                        }
                    })
                    .catch(function (error) {
                        alert("ERROR: " + (error.message | error));
                    });
            },
        refreshData() {
            this.isViewReady = false;

            axios.get('/api/info/accounts/' + this.$route.params.page)
                .then(response => {
                    this.accounts = response.data.accounts;
                    this.pages = response.data.pages;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const Account = {
    template: '' +
        '<div> <h3> {{account.code}} - {{account.name}} </h3>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<p>' +
                    '<router-link :to="portfolio">Портфель</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="load_deals">История сделок</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="load_positions">История позиций</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="result">Результаты</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="profit">Прибыль</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="value">Доходность</router-link>&nbsp&nbsp&nbsp' +
                '</p>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div > ',
    data() {
        return {           
            account: {},
            isViewReady: false
        };
    },
    computed: {
        portfolio() {
            return { name: 'portfolio', params: { id: this.account.id, account: this.account } };
        },
        load_deals() {
            return { name: 'account', params: { id: this.account.id, name: this.account.name, load: 'dealscsv' } };
        },
        load_positions() {
            return { name: 'account', params: { id: this.account.id, name: this.account.name, load: 'positionscsv' } };
        },
        result() {
            return { name: 'result', params: { id: this.account.id, account: this.account } };
        },
        profit() {
            return { name: 'profit', params: { id: this.account.id, account: this.account } };
        },
        value() {
            return { name: 'value', params: { id: this.account.id, account: this.account } };
        }
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            const id = this.$route.params.id;
            this.isViewReady = false;

            axios.get('/api/info/account/' + id)
                .then(response => {
                    this.account = response.data;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
            
            if (this.$route.params.load === undefined || this.$route.params.load === "") ;
            else
            {
                axios.request(
                    {
                        url: '/api/portfolio/' + this.$route.params.load + "/" +id,
                        method: 'GET',
                        responseType: 'blob'
                    })
                    .then(response => {
                        const downloadUrl = window.URL.createObjectURL(new Blob([response.data]));
                        const s = response.headers['content-disposition'].toString(); 
                        const filename = s.substring(s.indexOf('=')+1, s.indexOf('csv')+3);
                        
                        const link = document.createElement('a');
                        link.href = downloadUrl;
                        link.setAttribute('download', filename); 
                        document.body.appendChild(link);
                        link.click();
                        link.remove();
                    })
                    .catch(function (error) {
                        alert("ERROR: " + (error.message | error));
                    });
            }
        }
    },
    created() {
        this.refreshData();
    }
};

var AccountUtils = (function () {
    return {
        instrumentRefs: function (curve) {
            var refs = [];
            for (const { instrument_id, short_name, name, currency } of curve)
                refs.push({ id: instrument_id, name: '[' + short_name + '] ' + name, currency: currency });
            return refs;
        },
        getSeries: function (index, curve, itemsGetter) {
            var result = [];
            if (curve.length > 0 && index < curve.length) {
                for (const { t, v } of itemsGetter(curve[index])) {
                    var item = { x: new Date(t), y: v };
                    result.push(item);
                }
            }
            return result;
        },
        getPoints: function(curves, point, dataGetter) {
            var result = [];
            if (point.time > 0) {
                for (const pc of curves) {
                    const data = dataGetter(pc);
                    if (data.length > point.index && pc.instrument_id > 0) {
                        if (data[point.index].t === point.time) {
                            var item = { data: data[point.index].v, short_name: pc.short_name };
                            result.push(item);
                        }
                        else {
                            const step = (data[point.index].t < point.time) ? 1 : -1;
                            var index = point.index + step;
                            while (index >= 0 && index < data.length
                                && ((step > 0 && data[index].t < point.time)
                                    || (step < 0 && data[index].t > point.time))) {
                                index += step;
                            }

                            if (index >= 0 && index < data.length) {
                                item = { data: data[index].v, short_name: pc.short_name };
                                result.push(item);
                            }
                        }
                    }
                }
            }
            return result;
        },
        getAccountChartOptions: function (self, text, timeGetter) {
            return {
                chart: {
                    animations: { enabled: false },
                    selection: { enabled: false },
                    events: {
                        markerClick: (e, c, { seriesIndex, dataPointIndex }) => {
                            self.point_selected = {
                                time: timeGetter(seriesIndex, dataPointIndex),
                                index: dataPointIndex
                            };
                        }
                    }
                },
                stroke: { curve: 'straight', width: 2 },
                title: { text: text, align: 'left' },
                xaxis: { type: 'datetime' },
                yaxis: { tooltip: { enabled: false } }
            };
        },
        getChartPercentOptions: function (text, points) {
            var result = {
                chart: {
                    animations: { enabled: false },
                    selection: { enabled: false }
                },
                series: [],
                labels: [],
                title: { text: text, align: 'left' }
            };

            for (const { data, short_name } of points) {
                result.series.push(Math.abs(data));
                result.labels.push(short_name);
            }

            return result;
        },
        refreshCurveData: function (self, url) {
            self.isViewReady = false;
            axios.get(url)
                .then(response => {
                    self.curve = response.data.curve;

                    if (self.curve.length > 0) {
                        self.instrument_selected = self.curve.length - 1;
                        self.isViewReady = true;
                    }
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        },
        getTemplate: function (percent_chart) {
            return ''+
                '<div>' +
                    '<hr/>' +
                    '<div id="chart-box" v-if="isViewReady == true">' +
                        '<div id="instrument_select">Инструмент: '+
                            '<select v-model="instrument_selected" > ' +
                                '<option v-for="(i, idx) in instrumentRefs" :value="idx" >{{i.name}}</option>' +
                            '</select>' +
                        '</div > ' +
                        '<div style=\'width: 80%; float: left\'><apexchart type=line height=600 :options="chartOptions" :series="series" /></div>' +
                        '<div style=\'width: 20%; float: right\'> ' + percent_chart + '</div>' +
                    '</div><div v-else>' +
                        '<div>Loading data...</div>' +
                    '</div>' +
                '</div>';
        }
    };
})();


const AccountPortfolio = {
    template: '' +
        '<div>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<h4>позиции по счету {{$route.params.account.name}} </h4>' +
                '<div><h4>дата: </h4>' +
                    '<datepicker v-model="moment" name="m"></datepicker>' +
                '</div > ' +
                '<ul>' +
                    '<li v-for="p in positions">' +
                        '| {{p.place}} | {{p.id}} | {{p.short_name}} | {{p.bquantity}} | {{p.bquote}} | {{p.bvolume}} | {{p.quantity}} | {{p.price}} | {{p.comission}}| {{p.cost}} | {{p.quote}} | {{p.profit}} | {{p.qbuy}} | {{p.qsell}} | {{p.vbuy}} | {{p.vsell}} | {{p.pbuy}} | {{p.psell}} | {{p.reprice}} | {{p.currency}} |' +
                    '</li >' +
                '</ul>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div > ',
        components: { datepicker: vuejsDatepicker },
        data() {
            return {
                moment: new Date(this.$route.params.account.last),
                positions: [],
                isViewReady: false
            };
        },
        computed: {
        },
        watch: { '$route': 'refreshData', 'moment': 'refreshData' },
        methods: {
            refreshData() {
                this.isViewReady = false;

                axios.get(Globals.accountStateRequest(this.$route.params.id, this.moment.getTime()))
                    .then(response => {
                        this.positions = response.data;
                        this.isViewReady = true;
                    })
                    .catch(function (error) {
                        alert("ERROR: " + (error.message | error));
                    });
            }
        },
        created() {
            this.refreshData();
        }
};

const AccountDeals = {
    template: '' +
        '<div>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<h4>сделки по счету {{$route.params.account.name}} </h4>' +
                '<ul>' +
                    '<li v-for="d in deals">' +
                        '{{ new Date(d.date).toISOString() }} | {{d.type}} | {{d.instrument_id}} | {{d.short_name}}  | {{d.quantity}} | {{d.price}} | {{d.volume}} | {{d.currency}}' +
                    '</li >' +
            '</ul>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div > ',
    data() {
        return {
            deals: [],
            isViewReady: false
        };
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            const id = this.$route.params.id;
            this.isViewReady = false;

            axios.get('/api/portfolio/deals/' + id)
                .then(response => {
                    this.deals = response.data;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const AccountResult = {
    template: AccountUtils.getTemplate(
            '<apexchart type=pie height=200 :options="chartCostPercentOptions" />' +
            '<apexchart type=pie height=200 :options="chartValuePercentOptions" />' +
            '<apexchart type=pie height=200 :options="chartProfitPercentOptions" />'),
    components: { apexchart: VueApexCharts },
    data() {        
        return {
            isViewReady: false,
            instrument_selected: '',
            point_selected: { time: 0, index: 0 },
            curve: [],
            result: { cost: [], profit: [], value: [] }
        };
    },
    computed: {
        instrumentRefs() {
            return AccountUtils.instrumentRefs(this.curve);
        },
        series() {
            const getter = (itemsGetter) => AccountUtils.getSeries(this.instrument_selected, this.curve, itemsGetter);
            return [
                { name: 'Стоимость', data: getter(c => c.cost) },
                { name: 'Оценка', data: getter(c => c.value) },
                { name: 'Доход', data: getter(c => c.profit) }
            ];
        },
        chartOptions() {
            return AccountUtils.getAccountChartOptions(this, 'График результата ' + this.$route.params.account.name,
                (i, index) => {
                    const curve = this.curve[this.instrument_selected];
                    switch (i) {
                        case 0:
                            return curve.cost[index].t;
                        case 1:
                            return curve.value[index].t;
                        case 2:
                            return curve.profit[index].t;
                    }
                });
        },
        chartCostPercentOptions() {
            const time = this.point_selected.time;
            const text = (time > 0) ? 'График стоимости ' + new Date(time).toISOString() : '';
            return AccountUtils.getChartPercentOptions(text, this.result.cost);
        },
        chartValuePercentOptions() {
            const time = this.point_selected.time;
            const text = (time > 0) ? 'График оценки ' + new Date(time).toISOString() : '';
            return AccountUtils.getChartPercentOptions(text, this.result.value);
        },
        chartProfitPercentOptions() {
            const time = this.point_selected.time;
            const text = (time > 0) ? 'График дохода ' + new Date(time).toISOString() : '';
            return AccountUtils.getChartPercentOptions(text, this.result.profit);
        }
    },
    watch: {
        '$route': 'refreshData',
        instrument_selected() {
            if (this.$children.length > 0) {
                this.$children[0].refresh();
            }
        },
        point_selected() {
            const point = this.point_selected;
            if (point.time > 0) {
                const getter = itemsGetter => AccountUtils.getPoints(this.curve, point, itemsGetter);

                this.result = {
                    cost: getter(c => c.cost), profit: getter(c => c.profit), value: getter(c => c.value)
                };
            }
            else {
                this.result = { cost: [], profit: [], value: [] };
            }
        }
    },
    methods: {
        refreshData() {
            AccountUtils.refreshCurveData(this, Globals.accountResultRequest(this.$route.params.id));
        }
    },
    created() {
        this.refreshData();
    }
};

const AccountProfit = {
    template: AccountUtils.getTemplate(
            '<apexchart type=pie height=600 :options="chartPercentOptions" />'),
    components: { apexchart: VueApexCharts },
    data() {
        return {
            isViewReady: false,
            instrument_selected: '',
            point_selected: {time: 0, index: 0},
            curve: [],
            percent: []
        };
    },
    computed: {
        instrumentRefs() {
            return AccountUtils.instrumentRefs(this.curve);
        },
        series() {
            return [{ data: AccountUtils.getSeries(this.instrument_selected, this.curve, c => c.profit) }];
        },
        chartOptions()
        {
            return AccountUtils.getAccountChartOptions(this, 'График прибыли ' + this.$route.params.account.name,
                (i, index) => this.curve[this.instrument_selected].profit[index].t);
        },
        chartPercentOptions() {
            const time = this.point_selected.time;
            const text = (time > 0) ? 'Распределение прибыли ' + new Date(time).toISOString() : '';
            return AccountUtils.getChartPercentOptions(text, this.percent);
        }
    },
    watch: {
        '$route': 'refreshData',
        instrument_selected() {
            if (this.$children.length > 0) {
                this.$children[0].refresh();
            }
        },
        point_selected() {
            if (this.point_selected.time > 0) {
                this.percent = AccountUtils.getPoints(this.curve, this.point_selected, c => c.profit);
            }
            else {
                this.percent = [];
            }
        }
    },
    methods: {
        refreshData() {
            AccountUtils.refreshCurveData(this, Globals.accountProfitRequest(this.$route.params.id));
        }
    },
    created() {
        this.refreshData();
    }
};

const AccountValue = {
    template: AccountUtils.getTemplate(
        '<apexchart type=pie height=600 :options="chartPercentOptions" />'),
    components: { apexchart: VueApexCharts },
    data() {
        return {
            isViewReady: false,
            instrument_selected: '',
            point_selected: { time: 0, index: 0 },
            curve: [],
            value: []
        };
    },
    computed: {
        instrumentRefs() {
            return AccountUtils.instrumentRefs(this.curve);
        },
        series() {
            return [{ data: AccountUtils.getSeries(this.instrument_selected, this.curve, c => c.value) }];
        },
        chartOptions() {
            return AccountUtils.getAccountChartOptions(this, 'График оценки доходности ' + this.$route.params.account.name,
                (i, index) => this.curve[this.instrument_selected].value[index].t);
        },
        chartPercentOptions() {
            const time = this.point_selected.time;
            const text = (time > 0) ? 'Распределение доходности ' + new Date(time).toISOString() : '';
            return AccountUtils.getChartPercentOptions(text, this.value);
        }
    },
    watch: {
        '$route': 'refreshData',
        instrument_selected() {
            if (this.$children.length > 0) {
                this.$children[0].refresh();
            }
        },
        point_selected() {
            if (this.point_selected.time > 0) {
                this.value = AccountUtils.getPoints(this.curve, this.point_selected, c => c.value);
            }
            else {
                this.value = [];
            }
        }
    },
    methods: {
        refreshData() {
            AccountUtils.refreshCurveData(this, Globals.accountValueRequest(this.$route.params.id));
        }
    },
    created() {
        this.refreshData();
    }
};

const Instruments = {
    template: '' +
        '<div> <h3> Список инструментов </h3>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">'+
                '<div>Код инструмента: '+
                '<input v-model="instrument_selected" /><button v-on:click="viewInstrument">Ok</button>' +
                '</div > ' +
                '<p>' +
                    '<router-link v-for="p in pageRefs" :to="p">{{ p.params.page }}&nbsp&nbsp&nbsp</router-link>' +
                '</p>' +
                '<ul>' +
                    '<li v-for="i in instrumentRefs">' +
                        '<router-link :to="i">{{ i.params.name }}</router-link>' +
                    '</li >' +
                '</ul>' +
            '</div><div v-else>'+
                '<div>Loading data...</div>'+
            '</div>'+
        '</div>',
    data() {
        return {
            instruments: [],
            pages: 0,
            instrument_selected: '',
            isViewReady: false
        };
    },
    watch: { '$route': 'refreshData' },
    computed: {
        pageRefs() {
            var refs = [];
            for (var i = 1; i <= this.pages; i++)
                refs.push({ name: 'instruments', params: { page: i } });
            return refs;
        },
        instrumentRefs() {
            var refs = [];
            for (const i of this.instruments)
                refs.push({ name: 'instrument', params: { id: i.id, name: i.name, year: 2000 } });
            return refs;
        }
    },
    methods: {
        viewInstrument: function(event)
        {
            axios.get('/api/info/icode/' + this.instrument_selected)
                .then(response => {
                    if (response.data.id > 0)
                    {
                        router.push({ name: 'instrument', params: { id: response.data.id, name: response.data.name, year: response.data.year } });
                    }
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        },
        refreshData() {
            this.isViewReady = false;

            axios.get('/api/info/instruments/' + this.$route.params.page)
                .then(response => {
                    this.instruments = response.data.instruments;
                    this.pages = response.data.pages;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const Instrument = {
    template: '' +
        '<div> <h3>{{instrument.icode}} - {{instrument.code}} - {{instrument.name}} </h3>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<p>' +
                    '<router-link :to="quotes">Котировки</router-link>&nbsp&nbsp&nbsp' +
                    '<router-link :to="quotes_graph">График котировок</router-link>' +
                '</p>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div > ',
    data() {
        return {
            instrument: {},
            isViewReady: false
        };
    },
    watch: { '$route': 'refreshData' },
    computed: {
        quotes() {
            return { name: 'quotes', params: { id: this.$route.params.id, name: this.instrument.name, year: this.$route.params.year } };
        },
        quotes_graph() {
            return { name: 'quotes_graph', params: { id: this.$route.params.id, name: this.instrument.name, year: this.$route.params.year } };
        }
    },
    methods: {
        refreshData() {
            const id = this.$route.params.id;
            this.isViewReady = false;

            axios.get('/api/info/instrument/' + id)
                .then( response => {
                    this.instrument = response.data;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const InstrumentQuotes = {
    template: '' +
        '<div> <h3>Инструмент {{$route.params.name}}</h3>' +
            '<hr/>' +
            '<div v-if="isViewReady == true">' +
                '<h4>котировки инструмента {{$route.params.name}} за {{new Date(quotes[0].t).getFullYear()}} год</h4>' +
                '<p>' +
                    '<router-link v-for="y in yearRefs" :to="y">{{y.params.year}}&nbsp&nbsp&nbsp</router-link>' +
                '</p>' +
                '<ul>' +
                    '<li v-for="q in quotes">' +
                        '{{ new Date(q.t).toISOString() }} | {{q.v}} ' +
                    '</li >' +
                '</ul>' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div>',
    data() {
        return {
            quotes: [],
            years: [],
            isViewReady: false
        };
    },
    computed: {
        yearRefs() {
            var refs = [];
            for (const i of this.years)
                refs.push({ name: 'quotes', params: { id: this.$route.params.id, name: this.$route.params.name, year: i } });
            return refs;
        }
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            const id = this.$route.params.id + '/' + this.$route.params.year;
            this.isViewReady = false;

            axios.get('/api/info/quotes/' + id)
                .then(response => {
                    this.years = response.data.years;
                    this.quotes = response.data.quotes;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const InstrumentQuotesGraph = {
    template: '' +
        '<div> <h3>Инструмент {{$route.params.name}}</h3>' +
            '<hr/>' +
            '<div id="chart-box" v-if="isViewReady == true">' +
                '<p>' +
                '<router-link v-for="y in yearRefs" :to="y">{{ y.params.year }}&nbsp&nbsp&nbsp</router-link>' +
                '</p>' +
                '<apexchart type=line height=600 :options="chartOptions" :series="series" />' +
            '</div><div v-else>' +
                '<div>Loading data...</div>' +
            '</div>' +
        '</div>',
    components: { apexchart: VueApexCharts },
    data() {
        return {
            isViewReady: false,
            series: [],
            years: []            
        };
    },
    computed: {
        yearRefs() {
            var refs = [];
            for (const i of this.years)
                refs.push({ name: 'quotes_graph', params: { id: this.$route.params.id, name: this.$route.params.name, year: i } });
            return refs;
        },
        chartOptions() {
            return {
                chart: {
                    animations: { enabled: false },
                    selection: { enabled: false }
                },
                stroke: { curve: 'straight', width: 3 },
                title: { text: 'График котировок инструмента ' + this.$route.params.name, align: 'left' },
                xaxis: { type: 'datetime' },
                yaxis: { tooltip: { enabled: false } }
            };
        }
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            const id = this.$route.params.id + '/' + this.$route.params.year;
            this.isViewReady = false;

            axios.get('/api/info/quotes/' + id)
                .then(response => {
                    var quotes = [];
                    for (const q of response.data.quotes) {
                        var ohlc_item = { x: new Date(q.t), y: q.v };
                        quotes.push(ohlc_item);
                    }

                    this.series = [{ data: quotes }];
                    this.years = response.data.years;

                    this.chartOptions.title.text = 'График котировок инструмента ' + this.$route.params.name + ' за ' + quotes[0].x.getFullYear() + ' год';
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }  
};

const DemoLoader = {
    template: '' +
        '<div> <h3>Состояние загрузки демо-данных</h3>' +
        '<hr/>' +
        '<div id="state" v-if="isViewReady == true">' +
        '<ul>' +
        '<li v-for="s in state">{{ s.name }} => {{ s.loaded }} </li >' +
        '</ul>' +
        '<hr/>' +
        '<ul>' +
        '<li v-for="s in statistic">{{ s.name }} => {{ s.value }} </li >' +
        '</ul>' +

        '<router-link :to="loading">Загрузить</router-link>' +
        '</div><div v-else>' +
        '<div>Loading data...</div>' +
        '</div>' +
        '</div>',
    data() {
        return {
            isViewReady: false, state: [], statistic: []
        };
    },
    computed: {
        loading() {
            return { name: 'demoloader', params: { load: true } };
        }
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            this.isViewReady = false;

            axios.get('/api/data/demoloader?load=' + this.$route.params.load)
                .then(response => {
                    this.state = response.data.state;
                    this.statistic = response.data.statistic;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const DataLoader = {
    template: '' +
        '<div> <h3>Состояние загрузки тестовых данных</h3>' +
        '<hr/>' +
        '<div id="state" v-if="isViewReady == true">' +
        '<ul>' +
        '<li v-for="s in state">{{ s.name }} => {{ s.loaded }} </li >' +
        '</ul>' +
        '<hr/>' +
        '<ul>' +
        '<li v-for="s in statistic">{{ s.name }} => {{ s.value }} </li >' +
        '</ul>' +

        '<router-link :to="loading">Загрузить</router-link>' +
        '</div><div v-else>' +
        '<div>Loading data...</div>' +
        '</div>' +
        '</div>',
    data() {
        return {
            isViewReady: false, state: [], statistic: []
        };
    },
    computed: {
        loading() {
            return { name: 'dataloader', params: { load: true } };
        }
    },
    watch: { '$route': 'refreshData' },
    methods: {
        refreshData() {
            this.isViewReady = false;

            axios.get('/api/data/dataloader?load=' + this.$route.params.load)
                .then(response => {
                    this.state = response.data.state;
                    this.statistic = response.data.statistic;
                    this.isViewReady = true;
                })
                .catch(function (error) {
                    alert("ERROR: " + (error.message | error));
                });
        }
    },
    created() {
        this.refreshData();
    }
};

const routes = [
    { path: '/accounts/:page', components: { default: Accounts }, name: 'accounts' },
    { path: '/account/:id/:load', components: { default: Account }, name: 'account' },
    { path: '/portfolio/:id', components: { default: Account, chart: AccountPortfolio }, name: 'portfolio' },
    { path: '/deals/:id', components: { default: Account, chart: AccountDeals }, name: 'deals' },
    { path: '/profit/:id', components: { default: Account, chart: AccountProfit }, name: 'profit' },
    { path: '/value/:id', components: { default: Account, chart: AccountValue }, name: 'value' },
    { path: '/result/:id', components: { default: Account, chart: AccountResult }, name: 'result' },

    { path: '/instruments/:page', components: { default: Instruments }, name: 'instruments'  },
    { path: '/instrument/:id/:year', components: { default: Instrument }, name: 'instrument' },
    { path: '/quotes/:id/:year', components: { default: Instrument, chart: InstrumentQuotes }, name: 'quotes' },
    { path: '/quotes_graph/:id/:year', components: { default: Instrument, chart: InstrumentQuotesGraph }, name: 'quotes_graph' },

    { path: '/demoloader/:load', components: { default: DemoLoader }, name: 'demoloader' },
    { path: '/dataloader/:load', components: { default: DataLoader }, name: 'dataloader' },
];

const router = new VueRouter({ routes });

const app = new Vue({
    router
}).$mount('#app');